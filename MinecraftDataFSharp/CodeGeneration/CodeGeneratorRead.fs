module MinecraftDataFSharp.CodeGeneration.CodeGeneratorRead

open System.IO
open FSharp.Control
open Humanizer
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open MinecraftDataFSharp.CodeGeneration.Shared
open MinecraftDataFSharp.Models
open Protodef.Enumerable
open Protodef.Primitive
open Protodef

let private TypeToReadMethodMap =
    Map
        [ "bool", "ReadBoolean()"
          "i8", "ReadSignedByte()"
          "u8", "ReadUnsignedByte()"
          "i16", "ReadSignedShort()"
          "u16", "ReadUnsignedShort()"
          "i32", "ReadSignedInt()"
          "u32", "ReadUnsignedInt()"
          "i64", "ReadSignedLong()"
          "u64", "ReadUnsignedLong()"
          "f32", "ReadFloat()"
          "f64", "ReadDouble()"
          "UUID", "ReadUUID()"
          "restBuffer", "ReadToEnd()"
          "varint", "ReadVarInt()"
          "varlong", "ReadVarLong()"
          "string", "ReadString()"
          "pstring", "ReadString()" ]





let generateReadInstruct (field: ProtodefContainerField) =
    let csharpType = field.Type |> protodefTypeToCSharpType
    let name = field.Name.Camelize()
    let instructions = ResizeArray()

    let add (s: string) =
        SyntaxFactory.ParseStatement(s) |> instructions.Add

    let wi argName a =
        SyntaxFactory.ParseStatement($"reader.{a}();") |> instructions.Add

    let wiP m a =
        SyntaxFactory.ParseStatement($"reader.{a}(_protocolVersion);") |> instructions.Add

    let tmap = TypeToReadMethodMap
    let typeToMethod (t: ProtodefType) =
        tmap.TryFind(t.ToString())


    let rec generateInstruct (t: ProtodefType) (depth: int) =
        match typeToMethod t with
        | Some s -> s
        | None ->
            match t with
            | :? ProtodefOption as op ->
                let typeSharp = op |> protodefTypeToCSharpType
                let r = generateInstruct op.Type (depth + 1)
                let rName = $"r_{depth}"
                $"ReadOptional<{typeSharp}>({rName} => {rName}.{r})"
            | :? ProtodefBuffer as b ->
                if b.Rest = true then
                    "ReadToEnd()"
                else
                    "ReadBuffer()"
            | :? ProtodefArray as arr ->
                let typeSharp = arr.Type |> protodefTypeToCSharpType

            | _ -> failwith $"Unknown type {t}"


    ignore

let generateReadMethod (container: ProtodefContainer) =
    ignore


let createProperty (``type``: string) (name: string) =
    SyntaxFactory
        .PropertyDeclaration(SyntaxFactory.ParseTypeName(``type``), name)
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
        .AddAccessorListAccessors(
            SyntaxFactory
                .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            SyntaxFactory
                .AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
        )
    :> MemberDeclarationSyntax

let generateClass (container: ProtodefContainer) (name: string) =
    let fields =
        container.Fields
        |> Seq.map (fun x ->
            let type' = x.Type |> protodefTypeToCSharpType
            let name = x.Name.Pascalize()
            createProperty type' name)
        |> Array.ofSeq


    SyntaxFactory
        .ClassDeclaration(SyntaxFactory.Identifier(name))
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
        .AddMembers(fields)

let generateClasses (packet: Packet) =
    let name = packet.PacketName.Substring("packet_".Length).Pascalize()

    if packet.Structure.Count = 1 then
        let structure = packet.Structure |> Seq.head |> _.Value
        let cl = generateClass structure name
        [ cl ]
    else
        packet.Structure
        |> Seq.map (fun x ->
            let name = name + x.Key.ToString().Replace("-", "_")
            let structure = x.Value
            generateClass structure name)
        |> List.ofSeq

let generatePrimitive (packets: PacketMetadata list, folder: string) =
    let protodefPackets = packets |> Seq.map packetMetadataToProtodefPacket

    Directory.CreateDirectory(Path.Combine("packets", folder, "generated"))
    |> ignore

    for p in protodefPackets do
        let classes =
            p |> generateClasses |> Seq.cast<MemberDeclarationSyntax> |> Seq.toArray

        let packetName = p.PacketName
        let filePath = Path.Combine("packets", folder, "generated", $"{packetName}.cs")

        let ns =
            SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName("MinecraftDataFSharp"))
                .AddMembers(classes)

        File.WriteAllText(filePath, ns.NormalizeWhitespace().ToFullString())

    ignore
