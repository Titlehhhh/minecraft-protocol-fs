namespace PacketGenerator.Utils

open System
open Protodef
open PacketGenerator.Types

module VersionRange =
    let create ver = { StartVersion = ver; EndVersion = ver }

module Util =
    let equalTwoTypes (t1: ProtodefType option) (t2: ProtodefType option) =
        match t1, t2 with
        | Some t1, Some t2 -> t1.Equals(t2)
        | None, None -> true
        | _ -> false

module Filters =
    let isPacketMapper (s: string) =
        s.Equals("packet", StringComparison.OrdinalIgnoreCase)
    
    let isPacket (s: string) =
        s.StartsWith("packet", StringComparison.OrdinalIgnoreCase) && not (isPacketMapper s)