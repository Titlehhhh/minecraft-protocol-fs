module MinecraftDataFSharp.CodeGeneration.Shared

open System
open System.Collections.Generic
open System.Diagnostics
open System.Linq
open System.Text.Json
open System.Text.Json.Serialization
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

    options.Converters.Add(ProtodefTypeConverter())

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

let tryParseInt s =
    try
        s |> int |> Some
    with :? FormatException ->
        None

let rec getDefaultValue (t: ProtodefType) =
    match t with
    | :? ProtodefNumericType -> "0"
    | :? ProtodefCustomType -> "default"
    | :? ProtodefVarInt -> "0"
    | :? ProtodefVarLong -> "0"
    | :? ProtodefBool -> "false"
    | :? ProtodefOption -> "null"
    | :? ProtodefString -> "string.Empty"
    | :? ProtodefPrefixedString -> "string.Empty"
    | :? ProtodefBuffer as buff ->
        let count = if isNull (buff.Count) then "" else buff.Count.ToString()

        if String.IsNullOrWhiteSpace(count) then
            "[]"
        else
            match tryParseInt count with
            | Some x ->
                //[0, 0, 0, ... 0] x - count
                "[" + (Seq.replicate 0 "0" |> String.concat ",") + "]"
            | None -> $"new byte[{buff.Count}]"
    | :? ProtodefArray as arr ->
        let count = if isNull (arr.Count) then "" else arr.Count.ToString()

        if String.IsNullOrWhiteSpace(count) then
            "[]"
        else
            match tryParseInt count with
            | Some x ->
                let defValue = getDefaultValue arr.Type
                //[0, 0, 0, ... 0] x - count
                "[" + (Seq.replicate 0 defValue |> String.concat ",") + "]"
            | None -> $"new byte[{count}]"

let protocolVersionParameter =
    SyntaxFactory
        .Parameter(SyntaxFactory.Identifier("protocolVersion"))
        .WithType(SyntaxFactory.ParseTypeName("int"))




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

type Direction =
    | ToServer
    | ToClient

let generateClasses
    (
        packet: Packet,
        direction: Direction,
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



    let baseList =
        SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(name + "Packet"))

    let baseInterface =
        SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(baseInterface))


    let internalClasses =
        classes
        |> Seq.map (fun x ->
            let versions = fst x
            let attributeDeclaration = $"({versions.MinVersion}, {versions.MaxVersion})"

            let attrib =
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.ParseName("PacketSubInfo"),
                            SyntaxFactory.ParseAttributeArgumentList(attributeDeclaration)
                        )
                    )
                )

            let newName = versions.ToString().Replace("-", "_")
            let newName = $"V{newName}"
            
            let modifiers =
                SyntaxTokenList(
                    [| SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                       SyntaxFactory.Token(SyntaxKind.SealedKeyword)
                       SyntaxFactory.Token(SyntaxKind.PartialKeyword) |]
                )

            versions,
            (snd x)
                .WithIdentifier(SyntaxFactory.Identifier(newName))
                .AddBaseListTypes(baseList)
                .WithModifiers(modifiers)
                .AddAttributeLists(attrib))



    let generalProperties =
        getCommonProperties (internalClasses |> Seq.map (fun x -> snd x) |> Seq.toList)

    let generalPropertiesNames = getNames generalProperties

    let generalProperties =
        generalProperties |> List.map (fun x -> x :> MemberDeclarationSyntax)

    let internalClasses =
        internalClasses
        |> Seq.map (fun x ->
            let c = snd x

            let protoDef = packet.Structure.[fst x]

            let param = [ (fst x), protoDef ]
            let members = (generator param) |> Seq.toArray

            let newMembers =
                c.Members
                |> Seq.where (fun p -> not (containsProperty p generalProperties))
                |> Seq.toArray
                |> Array.append members

            c.WithMembers(SyntaxList<MemberDeclarationSyntax> newMembers))


    let members = generatorBase (generalPropertiesNames, packet)

    let membersWrap =
        generalProperties
        @ (internalClasses |> Seq.cast<MemberDeclarationSyntax> |> Seq.toList)
        @ members

    let modifiers =
        SyntaxFactory.TokenList(modifiers |> Seq.map (fun x -> SyntaxFactory.Token(x)) |> Seq.toArray)
    
    let packetPascalName = packet.PacketName.Substring("packet_".Length).Pascalize()
    let stage = "PacketState.Play"
    let direction = if direction = Direction.ToClient then "Clientbound" else "Serverbound"
    let direction = "PacketDirection." + direction
    let args = $"(\"{packetPascalName}\", {stage}, {direction})"
    
    let attrib =
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.ParseName("PacketInfo"),
                            SyntaxFactory.ParseAttributeArgumentList(args)
                        )
                    )
                )
    
    let wrapper =
        SyntaxFactory
            .ClassDeclaration(SyntaxFactory.Identifier(name + "Packet"))
            .AddMembers(membersWrap |> Array.ofSeq)
            .WithModifiers(modifiers)
            .AddBaseListTypes(baseInterface)
            .AddAttributeLists(attrib)

    [| wrapper |]



let private existsProp (cl: ClassDeclarationSyntax) =
    cl.Members |> Seq.exists (fun x -> x :? PropertyDeclarationSyntax)

let private existsProperty (classes: ClassDeclarationSyntax array) = classes |> Array.exists existsProp

let hideDuplicateCode (cl: ClassDeclarationSyntax, packet: Packet) : ClassDeclarationSyntax =
    

    let internalClasses = ResizeArray()

    for m in cl.Members do
        match m with
        | :? ClassDeclarationSyntax as c -> internalClasses.Add(c)
        | _ -> ()

    if internalClasses.Count = 0 || not (packet.EmptyRanges.IsEmpty) then
        cl
    else if existsProperty (internalClasses.ToArray()) then
        cl
    else
        let newMembers =
            cl.Members
            |> Seq.map (fun m ->
                match m with
                | :? ClassDeclarationSyntax as c ->
                    c.WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                            SyntaxFactory.Token(SyntaxKind.SealedKeyword),
                            SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                        )
                    )
                    :> MemberDeclarationSyntax
                | _ -> m)

        let newMembers = newMembers |> Seq.toArray

        cl.WithMembers(SyntaxList<MemberDeclarationSyntax> newMembers)
