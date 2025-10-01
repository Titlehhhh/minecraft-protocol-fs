namespace PacketGenerator.Extensions

open System.Collections.Generic
open Protodef

[<AutoOpen>]
module ProtodefProtocolExtensions =

    type ProtodefProtocol with
        /// TryFindType без comparer, возвращает Option
        member this.TryFindTypeOption(typeName: string) : ProtodefType option =
            let mutable t: ProtodefType = null
            if this.TryFindType(typeName, &t) && not (isNull t) then Some t
            else None

        /// TryFindType с IEqualityComparer, возвращает Option
        member this.TryFindTypeOption(typeName: string, comparer: IEqualityComparer<string>) : ProtodefType option =
            let mutable t: ProtodefType = null
            if this.TryFindType(typeName, comparer, &t) && not (isNull t) then Some t
            else None

