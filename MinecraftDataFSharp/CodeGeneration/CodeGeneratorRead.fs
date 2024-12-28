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
