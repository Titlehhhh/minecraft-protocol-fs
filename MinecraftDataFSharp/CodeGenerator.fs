module MinecraftDataFSharp.CodeGenerator

open System
open System.Collections.Generic
open System.IO
open System.Text
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

type VersionRange =
    { MinVersion: int
      MaxVersion: int }

    static member Parse(s: string) =
        let parts = s.Split('-')

        if parts.Length = 1 then
            { MinVersion = int parts.[0]
              MaxVersion = int parts.[0] }
        elif parts.Length = 2 then
            { MinVersion = int parts.[0]
              MaxVersion = int parts.[1] }
        else
            invalidArg "s" "Invalid version range format"

    override this.ToString() =
        if this.MinVersion = this.MaxVersion then
            this.MinVersion.ToString()
        else
            sprintf "%d-%d" this.MinVersion this.MaxVersion


type Packet =
    { PacketName: string
      Structure: Dictionary<VersionRange, ProtodefContainer>
      EmptyRanges: VersionRange list }

    member this.IsFullPacket = this.EmptyRanges.IsEmpty



let packetMetadataToProtodefPacket (packet: PacketMetadata) =
    let packetName = packet.PacketName

    let dict = Dictionary<VersionRange, ProtodefContainer>()
    let options = JsonSerializerOptions()
    
    options.Converters.Add(DataTypeConverter())

    packet.Structure
    |> Seq.filter (fun x -> x.Value.GetValueKind() <> JsonValueKind.String)
    |> Seq.map (fun x ->
        let fields = x.Value.Deserialize<List<ProtodefContainerField>>(options)
        let container = ProtodefContainer(fields)
        let versionRange = VersionRange.Parse x.Key
        versionRange, container)
    |> Seq.iter (fun (versionRange, container) -> dict.Add(versionRange, container))

    let emptyRanges =
        packet.Structure
        |> Seq.filter (fun x -> x.Value.ToJsonString() = "empty")
        |> Seq.map (fun x -> VersionRange.Parse x.Key)
        |> List.ofSeq

    { PacketName = packetName
      Structure = dict
      EmptyRanges = emptyRanges }




let NameToCSharpType =
    Map["ByteArray", "byte[]"
        "Slot", "Slot"
        "UUID", "Guid"
        "anonOptionalNbt", "NbtTag?"
        "anonymousNbt", "NbtTag"
        "bool", "bool"
        "buffer", "byte[]"
        "f32", "float"
        "f64", "double"
        "i16", "short"
        "i32", "int"
        "i64", "long"
        "i8", "sbyte"
        "nbt", "NbtTag"
        "optionalNbt", "NbtTag?"
        "optvarint", "int?"
        "position", "Position"
        "pstring", "string"
        "restBuffer", "byte[]"
        "slot", "Slot"
        "string", "string"
        "u16", "ushort"
        "u32", "uint"
        "u64", "ulong"
        "u8", "byte"
        "varint", "int"
        "varlong", "long"
        "vec2f", "Vector2"
        "vec3f", "Vector3"
        "vec3f64", "Vector3F64"
        "vec4f", "Vector4"]

let returnType = SyntaxFactory.ParseTypeName("ValueTask")

let rec protodefTypeToCSharpType (t: ProtodefType) =
    match t with
    | :? ProtodefNumericType as p -> p.NetName
    | :? ProtodefCustomType as p -> NameToCSharpType[p.Name]
    | :? ProtodefOption as p ->
        let deb = protodefTypeToCSharpType (p.Type)
        deb + "?"
    | :? ProtodefArray as p -> NameToCSharpType[protodefTypeToCSharpType (p.Type)] + "[]"
    | :? ProtodefVarInt -> "int"
    | :? ProtodefVarLong -> "long"
    | :? ProtodefPrefixedString -> "string"
    | :? ProtodefString -> "string"
    | :? ProtodefBool -> "bool"
    | :? ProtodefBuffer -> "byte[]"
    | _ -> failwith $"unknown type: {t}"


let TypeToWriteMethodOneArg =
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



let generateInstruct (field: ProtodefContainerField) =
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
            | _ -> failwith $"unknown custom type: {custom.Name}"

    generateWriteInstruction field.Type argName

    list

let generateBody (container: ProtodefContainer) =
    container.Fields |> Seq.collect generateInstruct |> Array.ofSeq

let generateMethod (range: VersionRange, container: ProtodefContainer) =
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

let generateMethods (packet: Packet) =
    let methods =
        packet.Structure
        |> Seq.map (fun x -> generateMethod (x.Key, x.Value))
        |> Array.ofSeq


    methods

let generatePrimitive (packets: PacketMetadata list) =
    let protodefPackets =
        packets
        |> Seq.where (fun x -> Extensions.IsPrimitive x.Structure)
        |> Seq.map packetMetadataToProtodefPacket

    Directory.CreateDirectory(Path.Combine("packets", "generated")) |> ignore

    for p in protodefPackets do
        let methods = generateMethods p

        let members =
            methods |> Seq.map (fun x -> x :> MemberDeclarationSyntax) |> Array.ofSeq

        let packetName = p.PacketName

        let filePath = Path.Combine("packets", "generated", $"{packetName}.cs")

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
