module MinecraftDataFSharp.CodeGeneration.CodeGeneratorWrite

open System
open System.Collections.Generic
open System.Diagnostics
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

let isAllEquivalent (classes: (VersionRange * ClassDeclarationSyntax) list) =
    match classes with
    | []
    | [ _ ] -> true // Пустой список или список с одним элементом
    | (_, firstClass) :: rest -> rest |> List.forall (fun (_, cl) -> cl.IsEquivalentTo(firstClass))

let getCommonProperties (classes: ClassDeclarationSyntax list) =

    let getProperties (classDecl: ClassDeclarationSyntax) =
        classDecl.Members
        |> Seq.choose (fun mem ->
            match mem with
            | :? PropertyDeclarationSyntax as prop -> Some prop
            | _ -> None)
        |> Seq.toList

    // Получаем список всех свойств для каждого класса
    let allProperties = classes |> List.map getProperties

    // Если список классов пустой, возвращаем пустой результат
    match allProperties with
    | [] -> []
    | firstProps :: rest ->
        // Находим свойства, которые эквивалентны во всех классах
        firstProps
        |> List.filter (fun prop ->
            rest
            |> List.forall (fun props -> props |> List.exists (fun otherProp -> prop.IsEquivalentTo(otherProp))))

let containsProperty (mem: MemberDeclarationSyntax) (props: MemberDeclarationSyntax list) =
    props
    |> List.exists (fun x ->
        let deb = x.IsEquivalentTo(mem)

        x.IsEquivalentTo(mem))

let private generateClasses (packet: Packet) =
    let name = packet.PacketName.Substring("packet_".Length).Pascalize()

    let classes =
        packet.Structure
        |> Seq.map (fun x -> x.Key, generateClass x.Value "EmptyClass")
        |> Seq.toList


    let eq = isAllEquivalent classes

    let baseList = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(name))

    if eq && packet.EmptyRanges.IsEmpty then
        let f = classes |> Seq.head
        let fClass = snd f
        [| fClass.WithIdentifier(SyntaxFactory.Identifier(name)) |]
    else
        let internalClasses =
            classes
            |> Seq.map (fun x ->
                let newName = (fst x).ToString().Replace("-", "_")
                let newName = $"V{newName}"

                let modifiers =
                    SyntaxTokenList(
                        [| SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                           SyntaxFactory.Token(SyntaxKind.SealedKeyword) |]
                    )

                (snd x)
                    .WithIdentifier(SyntaxFactory.Identifier(newName))
                    .AddBaseListTypes(baseList)
                    .WithModifiers(modifiers))


        let generalProperties =
            getCommonProperties (internalClasses |> Seq.toList)
            |> List.map (fun p -> p :> MemberDeclarationSyntax)



        let internalClasses =
            internalClasses
            |> Seq.map (fun c ->
                let newMembers =
                    c.Members
                    |> Seq.where (fun p ->

                        not (containsProperty p generalProperties))
                    |> Seq.toArray

                c.WithMembers(SyntaxList<MemberDeclarationSyntax> newMembers))



        let membersWrap =
            generalProperties
            @ (internalClasses |> Seq.cast<MemberDeclarationSyntax> |> Seq.toList)

        let modifiers = SyntaxTokenList([| SyntaxFactory.Token(SyntaxKind.PublicKeyword) |])

        let wrapper =
            SyntaxFactory
                .ClassDeclaration(SyntaxFactory.Identifier(name))
                .AddMembers(membersWrap |> Array.ofSeq)
                .WithModifiers(modifiers)

        [| wrapper |]

let generatePrimitive (packets: PacketMetadata list, folder: string) =
    let protodefPackets = packets |> Seq.map packetMetadataToProtodefPacket

    Directory.CreateDirectory(Path.Combine("packets", folder, "generated"))
    |> ignore

    for p in protodefPackets do
        let members =
            (generateClasses p) |> Seq.cast<MemberDeclarationSyntax> |> Seq.toArray

        let packetName = p.PacketName

        let filePath = Path.Combine("packets", folder, "generated", $"{packetName}.cs")


        let ns =
            SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName("MinecraftDataFSharp"))
                .AddMembers(members)

        File.WriteAllText(filePath, ns.NormalizeWhitespace().ToFullString())

    ignore
