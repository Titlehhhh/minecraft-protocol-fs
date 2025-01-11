module MinecraftDataFSharp.CodeGeneration.Shared

open System.Collections.Generic
open System.Diagnostics
open System.Text.Json
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open MinecraftDataFSharp
open MinecraftDataFSharp.Models
open Protodef
open Protodef.Converters
open Protodef.Enumerable
open Protodef.Primitive
open Humanizer

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
        |> Seq.filter (fun x -> x.Value.GetValueKind() = JsonValueKind.String)
        |> Seq.map (fun x -> VersionRange.Parse x.Key)
        |> List.ofSeq

    { PacketName = packetName
      Structure = dict
      EmptyRanges = emptyRanges }

let toProtodefPackets (packets: PacketMetadata list) =
    packets
    |> Seq.where (fun x -> Extensions.IsPrimitive x.Structure)
    |> Seq.map packetMetadataToProtodefPacket

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
        "vec4f", "Vector4"
        "MovementFlags", "byte"
        "PositionUpdateRelatives", "uint"
        "ContainerID", "int"]

let rec protodefTypeToCSharpType (t: ProtodefType) =
    match t with
    | :? ProtodefNumericType as p -> p.NetName
    | :? ProtodefCustomType as p -> NameToCSharpType[p.Name]
    | :? ProtodefOption as p ->
        let deb = protodefTypeToCSharpType p.Type
        deb + "?"
    | :? ProtodefArray as p ->
        let deb = protodefTypeToCSharpType p.Type
        let deb' = NameToCSharpType.TryFind deb

        match deb' with
        | Some x -> x + "[]"
        | None -> deb + "[]"
    | :? ProtodefVarInt -> "int"
    | :? ProtodefVarLong -> "long"
    | :? ProtodefPrefixedString -> "string"
    | :? ProtodefString -> "string"
    | :? ProtodefBool -> "bool"
    | :? ProtodefBuffer -> "byte[]"
    | _ -> failwith $"unknown type: {t}"

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

let getDefaultValue (t: ProtodefType) =
    match t with
    | :? ProtodefNumericType -> "0"
    | :? ProtodefCustomType -> "default"
    | _ -> "default"

let protocolVersionParameter =
    SyntaxFactory
        .Parameter(SyntaxFactory.Identifier("protocolVersion"))
        .WithType(SyntaxFactory.ParseTypeName("int"))

let generateSupportVersionsMethod (versions: VersionRange, addNew: bool) =
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

let generateSupportVersions (classes: string seq) =
    let versions =
        classes
        |> Seq.map (fun x -> $"{x}.SupportedVersion(protocolVersion)")
        |> String.concat " || "

    let returnStatement = SyntaxFactory.ParseStatement $"return {versions};"

    SyntaxFactory
        .MethodDeclaration(SyntaxFactory.ParseTypeName("bool"), "SupportedVersion")
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
        .AddParameterListParameters(protocolVersionParameter)
        .WithBody(SyntaxFactory.Block(returnStatement))
    :> MemberDeclarationSyntax


type MembersGenerator = (VersionRange * ProtodefContainer) list -> MemberDeclarationSyntax list
type MembersGeneratorForBase = string seq * Packet -> MemberDeclarationSyntax list


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

let private isAllEquivalent (classes: (VersionRange * ClassDeclarationSyntax) list) =
    match classes with
    | []
    | [ _ ] -> true
    | (_, firstClass) :: rest -> rest |> List.forall (fun (_, cl) -> cl.IsEquivalentTo(firstClass))

let private containsProperty (mem: MemberDeclarationSyntax) (props: MemberDeclarationSyntax list) =
    props |> List.exists _.IsEquivalentTo(mem)

let generateClasses
    (
        packet: Packet,
        modifiers: SyntaxKind seq,
        baseInterface: string,
        generator: MembersGenerator,
        generatorBase: MembersGeneratorForBase
    ) =
    let name = packet.PacketName.Substring("packet_".Length).Pascalize()

    let classes =
        packet.Structure
        |> Seq.map (fun x -> x.Key, generateClass x.Value "EmptyClass")
        |> Seq.toList



    let baseList = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(name))

    let baseInterface =
        SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(baseInterface))

    
    let internalClasses =
        classes
        |> Seq.map (fun x ->
            let newName = (fst x).ToString().Replace("-", "_")
            let newName = $"V{newName}"
            // public, and sealed
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
            
            let param = [(fst x), protoDef]
            let members = (generator param) |> Seq.toArray

            let newMembers =
                c.Members
                |> Seq.where (fun p -> not (containsProperty p generalProperties))
                |> Seq.toArray
                |> Array.append [| supportMethod |]
                |> Array.append members

            c.WithMembers(SyntaxList<MemberDeclarationSyntax> newMembers))

    let supportMethod =
        generateSupportVersions (internalClasses |> Seq.map (_.Identifier.Text))

    let members = generatorBase (generalPropertiesNames, packet)

    let membersWrap =
        generalProperties
        @ (internalClasses |> Seq.cast<MemberDeclarationSyntax> |> Seq.toList)
        @ [ supportMethod ]
        @ members

    let modifiers =
        SyntaxFactory.TokenList(modifiers |> Seq.map (fun x -> SyntaxFactory.Token(x)) |> Seq.toArray)

    let wrapper =
        SyntaxFactory
            .ClassDeclaration(SyntaxFactory.Identifier(name))
            .AddMembers(membersWrap |> Array.ofSeq)
            .WithModifiers(modifiers)
            .AddBaseListTypes(baseInterface)

    [| wrapper |]
