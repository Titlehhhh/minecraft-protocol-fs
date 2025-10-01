// For more information see https://aka.ms/fsharp-console-apps
open System
open Humanizer
open PacketGenerator.Core
open Protodef
open PacketGenerator.Extensions

let protoMap = ProtocolLoader.LoadProtocolsAsync(735, 772).GetAwaiter().GetResult()




let isPacket (s: string) = s.StartsWith("packet_") || s = "packet"

let filterTypes = [| "native" |]



let allTypes =
    protoMap.Protocols
    |> Seq.map _.Value.Protocol.GetAllTypes()
    |> Seq.concat
    |> Seq.filter (_.IsCustom(filterTypes) >> not)
    |> Seq.map _.ParentName
    |> Seq.filter (isPacket >> not)
    |> Seq.map _.Pascalize()
    |> Set



let fType = protoMap.Protocols.Values |> Seq.head

let comparer = PascalizeStringComparer.Instance

let gg = fType.Protocol.TryFindTypeOption("Slot", comparer)

printfn "%A" gg
