module MinecraftDataFSharp.CodeGeneration.CodeGeneratorWrite

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq.Expressions
open System.Text.Json
open Humanizer
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open MinecraftDataFSharp
open MinecraftDataFSharp.Models
open Protodef
open Protodef.Converters
open Protodef.Enumerable
open Protodef.Primitive
open Shared


let private TypeToWriteMethodMap =
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

let private generateInstructionForField (field: ProtodefContainerField) =
    let argName = field.Name.Camelize()

    let list = List<StatementSyntax>()

    let add (m: string) =
        SyntaxFactory.ParseStatement(m) |> list.Add

    let wi (m: string) (a: string) =
        SyntaxFactory.ParseStatement($"writer.{a}({m});") |> list.Add

    let wiP (m: string) (a: string) =
        SyntaxFactory.ParseStatement($"writer.{a}({m}, protocolVersion);") |> list.Add



    let rec generateWriteInstruction (t: ProtodefType) (name: string) : unit =
        match t with
        | :? ProtodefNumericType as p -> TypeToWriteMethodMap[p.OriginalName] |> wi name
        | :? ProtodefVarInt -> TypeToWriteMethodMap["varint"] |> wi name
        | :? ProtodefVarLong -> TypeToWriteMethodMap["varlong"] |> wi name
        | :? ProtodefString -> TypeToWriteMethodMap["string"] |> wi name
        | :? ProtodefPrefixedString -> TypeToWriteMethodMap["pstring"] |> wi name
        | :? ProtodefBool -> TypeToWriteMethodMap["bool"] |> wi name
        | :? ProtodefBuffer as b ->
            if b.Rest = true then
                TypeToWriteMethodMap["restBuffer"] |> wi name
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

let private generateSerializationInstructions (container: ProtodefContainer) =
    container.Fields |> Seq.collect generateInstructionForField |> Array.ofSeq



let private writerParameter =
    SyntaxFactory
        .Parameter(SyntaxFactory.Identifier("writer"))
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.RefKeyword))
        .WithType(SyntaxFactory.ParseTypeName("MinecraftPrimitiveWriter"))

let private protocolVersionParameter =
    SyntaxFactory
        .Parameter(SyntaxFactory.Identifier("protocolVersion"))
        .WithType(SyntaxFactory.ParseTypeName("int"))

let private generateSerializeInternalMethod (container: ProtodefContainer) =
    let parameters =
        container.Fields
        |> Seq.map (fun x ->
            let pascalCase = x.Name.Camelize()
            let csharpType = protodefTypeToCSharpType x.Type

            SyntaxFactory
                .Parameter(SyntaxFactory.Identifier(pascalCase))
                .WithType(SyntaxFactory.ParseTypeName(csharpType)))
        |> Seq.toList


    

    let parameters = (writerParameter :: protocolVersionParameter :: parameters) |> Seq.toArray

    let identifier = SyntaxFactory.Identifier("SerializeInternal")


    let ser = generateSerializationInstructions container

    let blockSyntax = SyntaxFactory.Block(ser)

    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), identifier)
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
        .AddParameterListParameters(parameters)
        .WithBody(blockSyntax)

let private generateSerializeMethod (container: ProtodefContainer) =
    let parameters =
        container.Fields |> Seq.map _.Name.Pascalize() |> String.concat ", "


    let body = $"SerializeInternal(writer, protocolVersion, {parameters});"

    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Serialize")
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
        .AddParameterListParameters(
            writerParameter,
            protocolVersionParameter
        )
        .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement(body)))
    :> MemberDeclarationSyntax






let private isAllEquivalent (classes: (VersionRange * ClassDeclarationSyntax) list) =
    match classes with
    | []
    | [ _ ] -> true
    | (_, firstClass) :: rest -> rest |> List.forall (fun (_, cl) -> cl.IsEquivalentTo(firstClass))

let private getCommonProperties (classes: ClassDeclarationSyntax list) =

    let getProperties (classDecl: ClassDeclarationSyntax) =
        classDecl.Members
        |> Seq.choose (fun mem ->
            match mem with
            | :? PropertyDeclarationSyntax as prop -> Some prop
            | _ -> None)
        |> Seq.toList

    let allProperties = classes |> List.map getProperties

    match allProperties with
    | [] -> []
    | firstProps :: rest ->
        firstProps
        |> List.filter (fun prop ->
            rest
            |> List.forall (fun props -> props |> List.exists (fun otherProp -> prop.IsEquivalentTo(otherProp))))

let private getNames (propeties: PropertyDeclarationSyntax list) = propeties |> List.map _.Identifier.Text

let private generateArgumentListForConcrete (generalProps: string seq, protodef: ProtodefContainer) =
    protodef.Fields
    |> Seq.map (fun f ->
        let pascalCase = f.Name.Pascalize()

        if generalProps |> Seq.contains pascalCase then
            pascalCase
        else
            getDefaultValue f.Type)
    |> Seq.toArray

let private generateBodyForBase (generalProps: string seq, containers: Dictionary<VersionRange, ProtodefContainer>) =
    let rec generateIfElse (remaining: (VersionRange * ProtodefContainer) list) =
        match remaining with
        | [] -> Some(SyntaxFactory.ParseStatement("throw new Exception();"))
        | (k, v) :: tail ->
            let arguments =
                generateArgumentListForConcrete (generalProps, v) |> String.concat ", "

            let className = $"V{k.ToString()}".Replace("-", "_")

            let condition =
                SyntaxFactory
                    .InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(className),
                            SyntaxFactory.IdentifierName("SupportedVersion")
                        )
                    )
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("protocolVersion"))
                            )
                        )
                    )

            let thenStatement =
                SyntaxFactory.ParseStatement($"{className}.SerializeInternal(writer, {arguments});")

            let elseClause = generateIfElse tail


            Some(
                SyntaxFactory
                    .IfStatement(condition, thenStatement)
                    .WithElse(SyntaxFactory.ElseClause(elseClause.Value))
            )

    let containerList = containers |> Seq.map (fun x -> x.Key, x.Value) |> Seq.toList
    (generateIfElse containerList).Value



let private generateSerializeMethodForBase
    (generalProps: string seq, containers: Dictionary<VersionRange, ProtodefContainer>)
    =

    let identifier = SyntaxFactory.Identifier("Serialize")


    let blockSyntax =
        SyntaxFactory.Block(generateBodyForBase (generalProps, containers))

    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), identifier)
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
        .AddParameterListParameters(writerParameter, protocolVersionParameter)
        .WithBody(blockSyntax)


let private containsProperty (mem: MemberDeclarationSyntax) (props: MemberDeclarationSyntax list) =
    props |> List.exists _.IsEquivalentTo(mem)

let private generateSupportVersionsMethod (versions: VersionRange, addNew: bool) =
    let identifier = SyntaxFactory.Identifier("SupportedVersion")

    let condition =
        SyntaxFactory.ParseStatement(
            if versions.MaxVersion = versions.MinVersion then
                $"return protocolVersion == {versions.MinVersion};"
            else
                $"return protocolVersion is >= {versions.MinVersion} and <= {versions.MaxVersion};"
        )

    let method =
        SyntaxFactory
            .MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), identifier)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(protocolVersionParameter)
            .WithBody(SyntaxFactory.Block(condition))

    let method =
        if addNew then
            method.AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword))
        else
            method

    method.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword)) :> MemberDeclarationSyntax


let private generateSupportVersions (classes: ClassDeclarationSyntax seq) =
    let versions =
        classes
        |> Seq.map (fun x -> $"{x.Identifier.ToFullString()}.SupportedVersion(protocolVersion)")
        |> String.concat " || "

    let returnStatement = SyntaxFactory.ParseStatement $"return {versions};"

    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "SupportedVersion")
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
        .AddParameterListParameters(protocolVersionParameter)
        .WithBody(SyntaxFactory.Block(returnStatement))
    :> MemberDeclarationSyntax

let private generateClasses (packet: Packet) =
    let name = packet.PacketName.Substring("packet_".Length).Pascalize()

    let classes =
        packet.Structure
        |> Seq.map (fun x -> x.Key, generateClass x.Value "EmptyClass")
        |> Seq.toList


    let eq = isAllEquivalent classes

    let baseList = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(name))

    let baseInterface =
        SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("IClientPacket"))

    if eq && packet.EmptyRanges.IsEmpty then
        let f = classes |> Seq.head
        let fClass = snd f
        let serializeMethod = generateSerializeMethod (packet.Structure.Values |> Seq.head)

        let serializeInternalMethod =
            generateSerializeInternalMethod (packet.Structure.Values |> Seq.head)

        [| fClass
               .WithIdentifier(SyntaxFactory.Identifier(name))
               .AddMembers(generateSupportVersionsMethod (AllVersion, false))
               .AddMembers(serializeInternalMethod, serializeMethod) |]
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

                fst x,
                (snd x)
                    .WithIdentifier(SyntaxFactory.Identifier(newName))
                    .AddBaseListTypes(baseList)
                    .WithModifiers(modifiers))


        let generalProperties =
            getCommonProperties (internalClasses |> Seq.map (fun x -> snd x) |> Seq.toList)

        let generalPropertiesNames = getNames generalProperties

        let generalProperties =
            generalProperties |> List.map (fun x -> x :> MemberDeclarationSyntax)

        let internalClasses =
            internalClasses
            |> Seq.map (fun x ->
                let c = snd x
                let supportMethod = generateSupportVersionsMethod ((fst x), true)
                let protoDef = packet.Structure.[fst x]
                let serializeMethod = generateSerializeMethod protoDef
                let serializeInternalMethod = generateSerializeInternalMethod protoDef

                let newMembers =
                    c.Members
                    |> Seq.where (fun p -> not (containsProperty p generalProperties))
                    |> Seq.toArray
                    |> Array.append [| supportMethod; serializeInternalMethod; serializeMethod |]

                c.WithMembers(SyntaxList<MemberDeclarationSyntax> newMembers))

        let supportMethod = generateSupportVersions internalClasses

        let serializeMethod =
            generateSerializeMethodForBase (generalPropertiesNames, packet.Structure)

        let membersWrap =
            generalProperties
            @ (internalClasses |> Seq.cast<MemberDeclarationSyntax> |> Seq.toList)
            @ [ supportMethod; serializeMethod ]

        let modifiers = SyntaxTokenList([| SyntaxFactory.Token(SyntaxKind.PublicKeyword) |])

        let wrapper =
            SyntaxFactory
                .ClassDeclaration(SyntaxFactory.Identifier(name))
                .AddMembers(membersWrap |> Array.ofSeq)
                .WithModifiers(modifiers)
                .AddBaseListTypes(baseInterface)

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
