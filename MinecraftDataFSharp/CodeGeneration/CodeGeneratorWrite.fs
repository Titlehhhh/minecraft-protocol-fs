module MinecraftDataFSharp.CodeGeneration.CodeGeneratorWrite

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open Humanizer
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open MinecraftDataFSharp.Models
open Protodef
open Protodef.Converters
open Protodef.Enumerable
open Protodef.Primitive
open Shared




let private TypeToWriteMethodOneArg =
    Map
        [ "bool", "WriteBoolean"
          "i8", "WriteSignedByte"
          "u8", "WriteUnsignedByte"
          "i16", "WriteSignedShort"
          "u16", "WriteUnsignedShort"
          "i32", "WriteSignedInt"
          "u32", "WriteUnsignedInt"
          "i64", "WriteSignedLong"
          "u64", "WriteUnsignedLong"
          "f32", "WriteFloat"
          "f64", "WriteDouble"
          "UUID", "WriteUUID"
          "restBuffer", "WriteBuffer"
          "varint", "WriteVarInt"
          "varlong", "WriteVarLong"
          "string", "WriteString"
          "pstring", "WriteString" ]



let private generateInstruct (field: ProtodefContainerField) =
    let argName = field.Name.Camelize()

    let list = List<StatementSyntax>()

    let add (m: string) =
        SyntaxFactory.ParseStatement(m) |> list.Add

    let wi (m: string) (a: string) =
        SyntaxFactory.ParseStatement($"writer.{a}({m});") |> list.Add

    let wiP (m: string) (a: string) =
        SyntaxFactory.ParseStatement($"writer.{a}({m}, _protocolVersion);") |> list.Add



    let rec generateWriteInstruction (t: ProtodefType) (name: string) : unit =
        match t with
        | :? ProtodefNumericType as p -> TypeToWriteMethodOneArg[p.OriginalName] |> wi name
        | :? ProtodefVarInt -> TypeToWriteMethodOneArg["varint"] |> wi name
        | :? ProtodefVarLong -> TypeToWriteMethodOneArg["varlong"] |> wi name
        | :? ProtodefString -> TypeToWriteMethodOneArg["string"] |> wi name
        | :? ProtodefPrefixedString -> TypeToWriteMethodOneArg["pstring"] |> wi name
        | :? ProtodefBool -> TypeToWriteMethodOneArg["bool"] |> wi name
        | :? ProtodefBuffer as b ->
            if b.Rest = true then
                TypeToWriteMethodOneArg["restBuffer"] |> wi name
            else
                "WriteVarInt" |> wi $"{argName}.Length"
                "WriteBuffer" |> wi name
        | :? ProtodefArray as a ->
            "WriteVarInt" |> wi $"{argName}.Length"

            $"foreach (var {argName}_item in {argName})" |> add
            generateWriteInstruction a.Type $"{argName}_item"
        | :? ProtodefOption as o ->
            "WriteBoolean" |> wi $"{argName} is not null"
            $"if ({argName} is not null)" |> add
            generateWriteInstruction o.Type $"{argName}!"
        | :? ProtodefCustomType as custom ->
            match custom.Name with
            | "vec2f" -> "WriteVector2" |> wiP name
            | "vec3f" -> "WriteVector3" |> wiP name
            | "vec3f64" -> "WriteVector364" |> wiP name
            | "vec4f" -> "WriteVector4" |> wiP name
            | "uuid" -> "WriteUUID" |> wi name
            | "position" -> "WritePosition" |> wiP name
            | "ByteArray" ->
                "WriteVarInt" |> wi $"{argName}.Length"
                "WriteBuffer" |> wi name
            | "slot" -> "WriteSlot" |> wiP name
            | "Slot" -> "WriteSlot" |> wiP name
            | "restBuffer" -> "WriteBuffer" |> wi name
            | "UUID" -> "WriteUUID" |> wi name
            | "MovementFlags" -> "WriteUnsignedByte" |> wi name
            | "PositionUpdateRelatives" -> "WriteUnsignedInt" |> wi name
            | "ContainerID" -> "WriteVarInt" |> wi name
            | _ -> failwith $"unknown custom type: {custom.Name}"
        | _ -> failwith $"Unknown type {t}"

    generateWriteInstruction field.Type argName

    list

let private generateBody (container: ProtodefContainer) =
    container.Fields |> Seq.collect generateInstruct |> Array.ofSeq

let private generateMethod (range: VersionRange, container: ProtodefContainer) =
    let parameters =
        container.Fields
        |> Seq.map (fun x ->
            let pascalCase = x.Name.Camelize()
            let csharpType = protodefTypeToCSharpType x.Type

            SyntaxFactory
                .Parameter(SyntaxFactory.Identifier(pascalCase))
                .WithType(SyntaxFactory.ParseTypeName(csharpType)))
        |> Array.ofSeq

    let identifier =
        SyntaxFactory.Identifier("Send" + range.ToString().Replace("-", "_"))


    let ser = generateBody container
    let blockSyntax = SyntaxFactory.Block(ser)

    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), identifier)
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
        .AddParameterListParameters(parameters)
        .WithBody(blockSyntax)

let private generateMethods (packet: Packet) =
    let methods =
        packet.Structure
        |> Seq.map (fun x -> generateMethod (x.Key, x.Value))
        |> Array.ofSeq


    methods

let generatePrimitive (packets: PacketMetadata list, folder: string) =
    let protodefPackets = packets |> Seq.map packetMetadataToProtodefPacket

    Directory.CreateDirectory(Path.Combine("packets", folder, "generated"))
    |> ignore

    for p in protodefPackets do
        let methods = generateMethods p

        let members =
            methods |> Seq.map (fun x -> x :> MemberDeclarationSyntax) |> Array.ofSeq

        let packetName = p.PacketName

        let filePath = Path.Combine("packets", folder, "generated", $"{packetName}.cs")

        let cl =
            SyntaxFactory
                .ClassDeclaration(packetName.Pascalize())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(members)

        let ns =
            SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName("MinecraftDataFSharp"))
                .AddMembers(cl)

        File.WriteAllText(filePath, ns.NormalizeWhitespace().ToFullString())

    ignore
