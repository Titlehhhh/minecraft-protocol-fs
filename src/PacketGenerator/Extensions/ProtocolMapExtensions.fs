namespace PacketGenerator.Extensions

open Humanizer
open PacketGenerator.Types
open PacketGenerator.Core
open Protodef
open PacketGenerator.Utils

[<AutoOpen>]
module ProtocolMapExtensions =

    type ProtocolMap with
        member this.AllTypesPath() =
            this.Protocols
            |> Seq.map _.Value.Protocol
            |> Seq.collect _.GetAllTypes()
            |> Seq.filter (not << _.IsCustom("native"))
            |> Seq.filter (fun x -> not (NameUtils.isPacketMapper x.ParentName))
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
                  Structure = x.Value.Protocol.tryFindByPath path  })
