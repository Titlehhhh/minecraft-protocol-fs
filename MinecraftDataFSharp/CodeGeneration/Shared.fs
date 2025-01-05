module MinecraftDataFSharp.CodeGeneration.Shared

open System.Collections.Generic
open System.Text.Json
open MinecraftDataFSharp.Models
open Protodef
open Protodef.Converters
open Protodef.Enumerable
open Protodef.Primitive

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
        "PositionUpdateRelatives", "uint"]

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
