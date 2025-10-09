module PacketGenerator.Protodef

open Protodef
open Protodef.Enumerable
open Protodef.Primitive

type ProtocolRoot = ProtodefProtocol

type ProtodefKind =
    | BitField of ProtodefBitField
    | BitFieldNode of ProtodefBitFieldNode
    | BitFlags of ProtodefBitFlags
    | ContainerField of ProtodefContainerField
    | CustomType of ProtodefCustomType
    | Loop of ProtodefLoop
    | Namespace of ProtodefNamespace
    | Option of ProtodefOption
    | PrefixedString of ProtodefPrefixedString
    | TopBitSetTerminatedArray of ProtodefTopBitSetTerminatedArray
    | Bool of ProtodefBool
    | NumericType of ProtodefNumericType
    | String of ProtodefString
    | VarInt of ProtodefVarInt
    | VarLong of ProtodefVarLong
    | Void of ProtodefVoid
    | Array of ProtodefArray
    | Buffer of ProtodefBuffer
    | Container of ProtodefContainer
    | CustomSwitch of ProtodefCustomSwitch
    | Mapper of ProtodefMapper
    | RegistryEntryHolder of ProtodefRegistryEntryHolder
    | RegistryEntryHolderSet of ProtodefRegistryEntryHolderSet
    | Switch of ProtodefSwitch
    | Unknown of ProtodefType

type BufferCount =
    | Rest
    | Fixed of int
    | Field of string
    | Number of ProtodefType

type ArrayCount =
    | Fixed of int
    | Field of string
    | Number of ProtodefType

[<AutoOpen>]
module ProtodefPatterns =
    let (|Protodef|) (t: ProtodefType) : ProtodefKind =
        match t with
        | :? ProtodefBitField as x -> BitField x
        | :? ProtodefBitFlags as x -> BitFlags x
        | :? ProtodefCustomType as x -> CustomType x
        | :? ProtodefLoop as x -> Loop x
        | :? ProtodefNamespace as x -> Namespace x
        | :? ProtodefOption as x -> Option x
        | :? ProtodefPrefixedString as x -> PrefixedString x
        | :? ProtodefTopBitSetTerminatedArray as x -> TopBitSetTerminatedArray x
        | :? ProtodefBool as x -> Bool x
        | :? ProtodefNumericType as x -> NumericType x
        | :? ProtodefString as x -> String x
        | :? ProtodefVarInt as x -> VarInt x
        | :? ProtodefVarLong as x -> VarLong x
        | :? ProtodefVoid as x -> Void x
        | :? ProtodefArray as x -> Array x
        | :? ProtodefBuffer as x -> Buffer x
        | :? ProtodefContainer as x -> Container x
        | :? ProtodefCustomSwitch as x -> CustomSwitch x
        | :? ProtodefMapper as x -> Mapper x
        | :? ProtodefRegistryEntryHolder as x -> RegistryEntryHolder x
        | :? ProtodefRegistryEntryHolderSet as x -> RegistryEntryHolderSet x
        | :? ProtodefSwitch as x -> Switch x
        | _ -> Unknown t

    let (|BufferCount|) (t: ProtodefBuffer) =
        if t.Rest.HasValue then
            Rest
        elif isNull t.CountType then
            BufferCount.Number t.CountType
        else
            match t.Count with
            | :? int as num -> BufferCount.Fixed num
            | :? string as s -> BufferCount.Field s
            | _ -> failwith $"Unknow {t.Count}"

    let (|ArrayCount|) (t: ProtodefArray) =
        if isNull t.CountType then
            ArrayCount.Number t.CountType
        else
            match t.Count with
            | :? int as num -> ArrayCount.Fixed num
            | :? string as s -> ArrayCount.Field s
            | _ -> failwith $"Unknow {t.Count}"
