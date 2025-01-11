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
          "pstring", "ReadString()"
          "vec2f", "ReadVector2(protocolVersion)"
          "vec3f", "ReadVector3(protocolVersion)"
          "vec3f64", "ReadVector364(protocolVersion)"
          "vec4f", "ReadVector4(protocolVersion)"
          "uuid", "ReadUUID()"
          "position", "ReadPosition(protocolVersion)"
          "ByteArray", "ReadBuffer()"
          "slot", "ReadSlot(protocolVersion)"
          "Slot", "ReadSlot(protocolVersion)"
          "anonymousNbt", "ReadNbtTag(false)"
          "anonOptionalNbt", "ReadOptionalNbt(false)"
          "nbt", "ReadNbtTag(true)"
          "optionalNbt", "ReadOptionalNbt(true)"
          "MovementFlags", "ReadUnsignedByte()"
          "PositionUpdateRelatives", "ReadUnsignedInt()"
          "ContainerID", "ReadVarInt()" ]

let ReadDelegateMap =
    Map
        [ "u8", "ReadDelegates.Byte"
          "i8", "ReadDelegates.SByte"
          "varint", "ReadDelegates.VarInt"
          "varlong", "ReadDelegates.VarLong"
          "i32", "ReadDelegates.Int32"
          "u32", "ReadDelegates.UInt32"
          "i64", "ReadDelegates.Int64"
          "u64", "ReadDelegates.UInt64"
          "i16", "ReadDelegates.Int16"
          "u16", "ReadDelegates.UInt16"
          "f32", "ReadDelegates.Float"
          "f64", "ReadDelegates.Double"
          "string", "ReadDelegates.String"
          "pstring", "ReadDelegates.String"
          "vec2f", "ReadDelegates.Vector2"
          "vec3f", "ReadDelegates.Vector3"
          "vec3f64", "ReadDelegates.Vector364" ]



let LengthFormatMap =
    Map
        [ "u8", "LengthFormat.Byte"
          "i8", "LengthFormat.Byte"
          "varint", "LengthFormat.VarInt"
          "i32", "LengthFormat.Int"
          "u32", "LengthFormat.Int"
          "i64", "LengthFormat.Int"
          "u64", "LengthFormat.Int"
          "i16", "LengthFormat.Short"
          "u16", "LengthFormat.Short" ]


let generateReadInstruct (field: ProtodefContainerField) =
    let name = field.Name.Pascalize()
    let tmap = TypeToReadMethodMap
    let typeToMethod (t: ProtodefType) = tmap.TryFind(t.ToString())


    let rec generateInstruct (t: ProtodefType) (depth: int) =
        match typeToMethod t with
        | Some s -> s
        | None ->
            match t with
            | :? ProtodefOption as op ->
                let typeSharp = op |> protodefTypeToCSharpType
                let r = generateInstruct op.Type (depth + 1)
                let rName = $"r_{depth}"

                match ReadDelegateMap.TryFind(op.Type.ToString()) with
                | Some x -> $"ReadOptional<{typeSharp}>({x})"
                | None -> $"ReadOptional<{typeSharp}>({rName} => {rName}.{r})"
            | :? ProtodefBuffer as b ->
                if b.Rest = true then
                    "ReadToEnd()"
                else
                    let length = LengthFormatMap[b.CountType.ToString()]
                    $"ReadBuffer({length})"
            | :? ProtodefArray as arr ->
                let typeSharp = arr.Type |> protodefTypeToCSharpType
                let r = generateInstruct arr.Type (depth + 1)
                let rName = $"r_{depth}"
                let length = LengthFormatMap[arr.CountType.ToString()]

                match ReadDelegateMap.TryFind(arr.Type.ToString()) with
                | Some x -> $"ReadArray<{typeSharp}, int>({length},{x})"
                | None -> $"ReadArray<{typeSharp}, int>({length},{rName} => {rName}.{r})"
            | :? ProtodefCustomType as c -> failwith $"unknown custom type: {c.Name}"
            | _ -> failwith $"Unknown type {t}"


    let generated = generateInstruct field.Type 0
    $"{name} = reader.{generated};" |> SyntaxFactory.ParseStatement

let private readerParameter =
    SyntaxFactory
        .Parameter(SyntaxFactory.Identifier("reader"))
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.RefKeyword))
        .WithType(SyntaxFactory.ParseTypeName("MinecraftPrimitiveReader"))

let generateReadMethod (container: ProtodefContainer) =
    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), SyntaxFactory.Identifier("Deserialize"))
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
        .AddParameterListParameters(readerParameter, protocolVersionParameter)
        .AddBodyStatements(container.Fields |> Seq.map generateReadInstruct |> Array.ofSeq)

let generateDeserializeMethod (container: ProtodefContainer) =
    [ (generateReadMethod container) :> MemberDeclarationSyntax ]

let generateDeserializeMethodForBase (_: string seq, _: Packet) =
    let identifier = SyntaxFactory.Identifier("Deserialize")

    [ SyntaxFactory
          .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), identifier)
          .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AbstractKeyword))
          .AddParameterListParameters(readerParameter, protocolVersionParameter)
          .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
      :> MemberDeclarationSyntax ]


let generatePrimitive (packets: PacketMetadata list, folder: string) =
    let protodefPackets = packets |> Seq.map packetMetadataToProtodefPacket

    Directory.CreateDirectory(Path.Combine("packets", folder, "generated"))
    |> ignore

    for p in protodefPackets do
        let members =
            (generateClasses (
                p,
                [| SyntaxKind.PublicKeyword; SyntaxKind.AbstractKeyword |],
                "IServerPacket",
                generateDeserializeMethod,
                generateDeserializeMethodForBase
            ))
            |> Seq.cast<MemberDeclarationSyntax>
            |> Seq.toArray

        let packetName = p.PacketName

        let filePath = Path.Combine("packets", folder, "generated", $"{packetName}.cs")


        let ns =
            SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName("MinecraftDataFSharp"))
                .AddMembers(members)                

        File.WriteAllText(filePath, ns.NormalizeWhitespace().ToFullString())

    ignore
