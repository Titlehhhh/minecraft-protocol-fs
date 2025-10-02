// For more information see https://aka.ms/fsharp-console-apps
open System
open System.Text.Json.Nodes
open System.Threading.Tasks
open System.Xml.Linq
open Humanizer
open PacketGenerator.Core
open Protodef
open PacketGenerator.Extensions







exit 0

let protoMap =
    ProtocolLoader.LoadProtocolsAsync(735, 772)
    |> Async.AwaitTask
    |> Async.RunSynchronously


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
