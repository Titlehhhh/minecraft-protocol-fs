module MinecraftDataFSharp.CodeGenerator

open System
open System.Collections.Generic
open System.Text.Json
open Humanizer
open Microsoft.CodeAnalysis.CSharp
open MinecraftDataFSharp.Models
open Protodef
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

    packet.Structure
    |> Seq.filter (fun x -> x.Value.ToJsonString() <> "empty")
    |> Seq.map (fun x ->
        let fields = x.Value.Deserialize<List<ProtodefContainerField>>()
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
    Map[
        "ByteArray", "byte[]"
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
        ]
    
let returnType = SyntaxFactory.ParseTypeName("ValueTask")

let rec protodefTypeToCSharpType (t: ProtodefType) =
    match t with
    | :? ProtodefNumericType as p -> p.NetName
    | :? ProtodefCustomType as p -> NameToCSharpType[p.Name]
    | :? ProtodefOption as p -> NameToCSharpType[protodefTypeToCSharpType(p.Type)]+"?"
    | :? ProtodefArray as p -> NameToCSharpType[protodefTypeToCSharpType(p.Type)]+"[]"
    | :? ProtodefVarInt -> "int"
    | :? ProtodefVarLong -> "long"
    | :? ProtodefPrefixedString -> "string"
    | :? ProtodefString -> "string"
    | :? ProtodefBool -> "bool"
    | :? ProtodefBuffer -> "byte[]"
    | _ -> failwith $"unknown type: {t}"
    
        //| :? Protodef
    
let generateMethod (range: VersionRange, container: ProtodefContainer) =
    let parameters = container.Fields |> Seq.map (fun x ->
        let pascalCase = x.Name.Pascalize()
        let csharpType = protodefTypeToCSharpType x.Type
        SyntaxFactory.Parameter(SyntaxFactory.Identifier(pascalCase))
            .WithType(SyntaxFactory.ParseTypeName(csharpType))
        )
    
    ignore

let generateMethods (packet: Packet) = ignore

let generatePrimitive (packets: PacketMetadata list) =
    let protodefPackets =
        packets
        |> Seq.where (fun x -> Extensions.IsPrimitive x.Structure)
        |> Seq.map packetMetadataToProtodefPacket

    ignore
