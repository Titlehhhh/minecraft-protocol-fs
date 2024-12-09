open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net.Http
open System.Net.Http.Json
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core
open MinecraftDataFSharp
open MinecraftDataFSharp.MinecraftDataParser
open MinecraftDataFSharp.Models
open OllamaSharp
open OllamaSharp.Models
open Semver


let protocols = MinecraftDataParser.getPcProtocols

ProtocolTypeMapper.generateVersionedTypeMap protocols

let answer = QwenService.Predict ("Quick sort in C#","You C# Programmer") |> Async.RunSynchronously

match answer with
| Some x -> printfn "%s" x
| None -> printfn "None"

type VerAndPacket = { Version: int; JsonPacket: JsonNode }

let toTestPacket (node: ProtocolVersionEntry, packetName: string) : VerAndPacket =
    try
        let a = node.JsonProtocol["play"]["toClient"]["types"][packetName][1]

        { Version = node.ProtocolVersion
          JsonPacket = a }
    with :? Exception ->
        { Version = node.ProtocolVersion
          JsonPacket = JsonValue.Create("empty") }


let allServerboundPackets = HashSet<string>()



protocols
|> Seq.map (fun x -> x.JsonProtocol["play"]["toClient"]["types"])
|> Seq.iter (fun x ->
    x.AsObject()
    |> Seq.where (_.Key.StartsWith("packet_"))
    |> Seq.iter (fun g -> allServerboundPackets.Add(g.Key) |> ignore))



Directory.CreateDirectory("toClientPackets") |> ignore

for packet in allServerboundPackets do

    let listPackets =
        protocols |> Seq.map (fun t -> toTestPacket (t, packet)) |> Seq.toArray

    let mutable old = listPackets[0]
    let mutable firstVer = old.Version
    let obj = JsonObject()

    for i = 1 to listPackets.Length - 1 do
        let curr = listPackets[i]

        if not (JsonObject.DeepEquals(curr.JsonPacket, old.JsonPacket)) then
            let fromVer = firstVer
            let toVer = listPackets[i - 1].Version

            let key =
                if fromVer = toVer then
                    fromVer.ToString()
                else
                    $"{fromVer}-{toVer}"

            let clone = old.JsonPacket.DeepClone()
            obj.Add(key, clone)
            firstVer <- curr.Version
            old <- curr

    let fromVer = old.Version
    let toVer = (listPackets |> Array.last).Version

    let key =
        if fromVer = toVer then
            fromVer.ToString()
        else
            $"{fromVer}-{toVer}"

    obj.Add(key, old.JsonPacket.DeepClone())
    let resultJson = obj.ToJsonString(JsonSerializerOptions(WriteIndented = true))
    File.WriteAllText(Path.Combine("toClientPackets", $"{packet}.json"), resultJson)
