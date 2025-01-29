open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open Humanizer
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core
open MinecraftDataFSharp
open MinecraftDataFSharp.CodeGeneration
open MinecraftDataFSharp.Models
open Protodef
open Microsoft.CodeAnalysis

let protocols = MinecraftDataParser.getPcProtocols


type PacketProtocolMapping =
    { PacketName: string
      ProtocolVersion: int
      Identifier: string }

let generateIds
    (protocol: ProtocolVersionEntry)
    (state: string)
    (side: string)
    : (array<string> * seq<PacketProtocolMapping>) option =
    let json = protocol.JsonProtocol.AsObject()

    match json.TryGetPropertyValue(state) with
    | false, _ -> None
    | true, stateData ->
        let sideData = stateData[side]

        let packetMappings =
            (sideData["types"]["packet"][1][0]["type"][1]["mappings"]).AsObject()

        let packetPrefix =
            let s = state.Pascalize()

            if side = "toServer" then
                $"Client{s}Packet"
            else
                $"Server{s}Packet"

        let uniquePackets = HashSet<string>()

        let mappings =
            seq {
                for KeyValue(k, v) in packetMappings do
                    let packetName = $"{packetPrefix}.{v.ToString().Pascalize()}"
                    uniquePackets.Add(v.ToString().Pascalize()) |> ignore

                    yield
                        { PacketName = packetName
                          ProtocolVersion = protocol.ProtocolVersion
                          Identifier = k }
            }
            |> Seq.toArray

        Some(uniquePackets |> Array.ofSeq, mappings)

let save (packets: PacketMetadata list) (direction: string) (folderName: string) =
    let folder = folderName.Pascalize()
    Directory.CreateDirectory(folder) |> ignore
    Directory.CreateDirectory(Path.Combine(folder, direction)) |> ignore

    packets
    |> Seq.iter (fun packet ->
        let filePath = Path.Combine(folder, direction, $"{packet.PacketName}.json")
        File.WriteAllText(filePath, packet.Structure.ToJsonString(JsonSerializerOptions(WriteIndented = true))))

for direction in [| "toClient"; "toServer" |] do
    for state in [| "login"; "status"; "configuration"; "handshaking" |] do

        let packets = JsonPacketGenerator.generatePackets protocols direction state
        save packets direction state

for direction in [| "toClient"; "toServer" |] do
    for state in [| "login"; "status"; "configuration"; "play";"handshaking" |] do
        let idsSeq =
            protocols
            |> Seq.map (fun p -> generateIds p state direction)
            |> Seq.choose id
            |> Seq.toArray

        let ids =
            idsSeq
            |> Seq.map snd
            |> Seq.concat
            |> Seq.map (fun p -> $"{{Combine({p.PacketName}, {p.ProtocolVersion}), {p.Identifier}}},")

        let path = Path.Combine(state.Pascalize(), direction)
        Directory.CreateDirectory(path) |> ignore

        (let path = Path.Combine(path, "ids.txt")
         File.WriteAllLines(path, ids))


        let name =
            if direction = "toServer" then
                "ServerPacket"
            else
                "ClientPacket"

        let direction =
            if direction = "toServer" then
                "Serverbound"
            else
                "Clientbound"

        let content =
            idsSeq
            |> Seq.map fst
            |> Seq.concat
            |> Set
            |> Seq.mapi (fun i x ->
                let name = x.Pascalize()
                let state = "PacketState." + state.Pascalize()
                let direction = "PacketDirection." + direction
                $"\tpublic static PacketIdentifier {name} => new ({i}, nameof({name}),{state},{direction});")
            |> String.concat "\n"

        let className = if direction = "Serverbound" then "Client" else "Server"

        let className = $"{className}{state.Pascalize()}Packet"


        let content =
            StringBuilder()
                .AppendLine($"public static class {className}")
                .AppendLine("{")
                .AppendLine(content)
                .AppendLine("}")
                .ToString()




        let name = $"{name}.Generated{state.Pascalize()}.cs"

        (let path = Path.Combine(path, name)
         File.WriteAllText(path, content))






ProtocolTypeMapper.generateVersionedTypeMap protocols



if Directory.Exists("packets") then
    Directory.EnumerateFiles("packets", "*.*", SearchOption.AllDirectories)
    |> Seq.iter (fun filePath ->
        try
            File.Delete(filePath)
        with _ ->
            ())

let generate (side: string) =
    let packets = JsonPacketGenerator.generatePackets protocols side "play"

    let filterPrimitivePackets (packet: PacketMetadata) = Extensions.IsPrimitive packet.Structure


    let packetFolders = [ "primitive"; "complex" ]

    packetFolders
    |> Seq.iter (fun folder ->
        let dirPath = Path.Combine("packets", side, folder)
        Directory.CreateDirectory(dirPath) |> ignore)


    packets
    |> Seq.iter (fun packet ->
        let folder =
            if filterPrimitivePackets packet then
                "primitive"
            else
                "complex"

        let filePath = Path.Combine("packets", side, folder, $"{packet.PacketName}.json")
        File.WriteAllText(filePath, packet.Structure.ToJsonString(JsonSerializerOptions(WriteIndented = true))))

    let primitivePackets = packets |> Seq.filter filterPrimitivePackets |> List.ofSeq

    if side = "toServer" then
        CodeGeneratorWrite.generatePrimitive (primitivePackets, side) |> ignore
    else
        CodeGeneratorRead.generatePrimitive (primitivePackets, side) |> ignore

        let protodefPackets =
            primitivePackets |> Seq.map Shared.packetMetadataToProtodefPacket

        let data = PacketFactoryGenerator.create (protodefPackets, protocols)
        let identifier = SyntaxFactory.Identifier("PacketFactory")

        let factories =
            data.Factories
            |> Seq.map (fun x -> SyntaxFactory.ParseMemberDeclaration(x))
            |> Seq.toArray

        let keyvalues = data.Dict |> String.concat "\n,"

        let g =
            $"private static readonly FrozenDictionary<long, Func<IServerPacket>> ClientboundPackets = new Dictionary<long,Func<IServerPacket>> {{ {keyvalues} }}.ToFrozenDictionary();"

        let field = (SyntaxFactory.ParseMemberDeclaration g)

        let packetFactoryClass =
            SyntaxFactory
                .ClassDeclaration(identifier)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                )
                .AddMembers(factories)
                .AddMembers(field)

        let ns =
            SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName("McProtoNet.Protocol"))
                .AddMembers(packetFactoryClass)

        let compileUnit =
            SyntaxFactory
                .CompilationUnit()
                .AddMembers(ns)
                .AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("McProtoNet.Protocol.ClientboundPackets.Play")),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Frozen"))
                )

        File.WriteAllText("PacketFactory.Generated.Play.cs", compileUnit.NormalizeWhitespace().ToFullString())
        ()


generate "toServer"
generate "toClient"




//Generate login, status, configuration packets
