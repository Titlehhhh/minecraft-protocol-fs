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

ProtocolTypeMapper.generateVersionedTypeMap protocols
let allPackets = HashSet<string>()



let generateIds (protocol) (side: string) (protocolVersion: int) =
    let obj =
        (protocol.JsonProtocol["play"][side]["types"]["packet"][1][0]["type"][1]["mappings"])
            .AsObject()

    let name = if side = "toServer" then "ClientPacket" else "ServerPacket"

    for KeyValue(k, v) in obj do
        allPackets.Add(v.ToString().Pascalize()) |> ignore
        printfn $"{{Combine({name}.{v.ToString().Pascalize()},{protocolVersion}), {k}}},"




//protocols |> Seq.iter (fun x -> generateIds x "toClient" x.ProtocolVersion)

//printfn ""

//allPackets |> Seq.iteri (fun i x ->  printfn $"%s{x} = %d{i},")


if Directory.Exists("packets") then
    Directory.EnumerateFiles("packets", "*.*", SearchOption.AllDirectories)
    |> Seq.iter (fun filePath ->
        try
            File.Delete(filePath)
        with _ ->
            ())

let generate (side: string) =
    let packets = JsonPacketGenerator.generatePackets protocols side

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
        let protodefPackets = packets |> Seq.map Shared.packetMetadataToProtodefPacket
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
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("McProtoNet.Protocol.ClientboundPackets"))
                )

        File.WriteAllText("PacketFactory.Generated.cs", compileUnit.NormalizeWhitespace().ToFullString())
        ()


generate "toServer"
generate "toClient"


Extensions.PrintUnknownTypes()

// let firt10Packets = packets |> Seq.take 10 |> Seq.map (fun x -> createPromt x)
//
// let systemMessage = "You C# code generator for Minecraft protocol library"
// Directory.CreateDirectory("packets") |> ignore
// printfn "Start generation"
//
// packets
// |> Seq.take 10
// |> Seq.map (fun info ->
//     let promt = createPromt info
//
//     async {
//         let! result = LLMService.Predict(promt, systemMessage)
//         let filePath = Path.Combine("packets", $"{info.PacketName}.cs")
//
//         match result with
//         | Some x -> return! File.WriteAllTextAsync(filePath, x) |> Async.AwaitTask
//         | None -> return ()
//     })
// |> Async.Parallel
// |> Async.RunSynchronously
// |> ignore
