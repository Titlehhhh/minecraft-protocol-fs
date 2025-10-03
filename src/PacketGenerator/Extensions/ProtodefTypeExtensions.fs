namespace PacketGenerator.Extensions

open System.Collections.Generic
open Protodef

[<AutoOpen>]
module ProtodefTypeExtensions =
    type ProtodefProtocol with
        member this.TryFindTypeOption(typeName: string) : ProtodefType option =
            let mutable t: ProtodefType = null

            if this.TryFindType(typeName, &t) && not (isNull t) then
                Some t
            else
                None

        member this.TryFindTypeOption(typeName: string, comparer: IEqualityComparer<string>) : ProtodefType option =
            let mutable t: ProtodefType = null

            if this.TryFindType(typeName, comparer, &t) && not (isNull t) then
                Some t
            else
                None

    type ProtodefType with
        member this.tryFindByPath path = this.GetByPath(path) |> Option.ofObj
