namespace PacketGenerator.CodeGeneration

open System
open Protodef
open Protodef.Enumerable
open Protodef.Primitive


module Mapping =
    
    let TypeToWriteMethodMap =
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
    let tryParseInt s =
        try
            s |> int |> Some
        with :? FormatException ->
            None
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