module Test

open System
open System.Runtime.InteropServices
open Humanizer
open PacketGenerator.Core
open PacketGenerator.Extensions
open Protodef

type NamePathPair = { Name: string; Path: string }

let private isPacketMapper (s: string) =
    s.Equals("packet", StringComparison.OrdinalIgnoreCase)


let private findTypeByPath (path: string) (protocol: ProtodefProtocol) : ProtodefType option =
    protocol.GetByPath(path) |> Option.ofObj


type TypeFinderResult =
    { Version: int
      Structure: ProtodefType option }

type ProtocolMap with
    member this.AllTypesPath() =
        this.Protocols
        |> Seq.map _.Value.Protocol
        |> Seq.collect _.GetAllTypes()
        |> Seq.filter (not << _.IsCustom("native"))
        |> Seq.filter (fun x -> not (isPacketMapper x.ParentName))
        |> Seq.map (fun x ->
            { Name = x.ParentName.Pascalize()
              Path = x.Path })
        |> Set
        |> Set.toArray
        |> Array.sortBy _.Name

    member this.findTypesByPath(path: string) =
        this.Protocols
        |> Seq.map (fun x ->
            { Version = x.Key
              Structure = findTypeByPath path x.Value.Protocol })
