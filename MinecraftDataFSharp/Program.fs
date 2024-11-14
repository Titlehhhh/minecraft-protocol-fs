open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core
open Semver


// For more information see https://aka.ms/fsharp-console-apps

[<Literal>]
let minecraftDataPath = "minecraft-data"

type ProtocolNode =
    { ProtocolVersion: int
      MinVersion: string
      MaxVersion: string
      JsonProtocol: JsonNode }

let dataPaths =
    Path.Combine(minecraftDataPath, "data", "dataPaths.json")
    |> File.ReadAllText
    |> JsonObject.Parse

let getProtocolVersion (node: JsonNode) : int =
    let verPath = (node.AsObject()["version"]).ToString()

    let version =
        (Path.Combine(minecraftDataPath, "data", verPath, "version.json")
         |> File.ReadAllText
         |> JsonObject.Parse)
            .AsObject()

    (version["version"]).ToString() |> int

let getJsonProtocol (node: JsonNode) : JsonNode =
    let protoPath = (node.AsObject()["protocol"]).ToString()

    Path.Combine(minecraftDataPath, "data", protoPath, "protocol.json")
    |> File.ReadAllText
    |> JsonObject.Parse

let toProtocols (dataPaths: JsonObject) =

    let grouped =
        dataPaths
        |> Seq.groupBy (fun x -> getProtocolVersion x.Value)
        |> Seq.where (fun x -> x |> fst >= 340)

    [ for tuple in grouped do
          let protoVer = fst tuple
          let versions = snd tuple
          let protocolNode = versions |> Seq.map (_.Value) |> Seq.head |> getJsonProtocol
          
          
          let onlyVersions = versions |> Seq.map (_.Key) |> Seq.toArray
          let minVersion = onlyVersions |> Array.head
          let maxVersion = onlyVersions[onlyVersions.Length - 1]
          

          let node: ProtocolNode =
              { ProtocolVersion = protoVer
                MinVersion = minVersion
                MaxVersion = maxVersion
                JsonProtocol = protocolNode }
          if protoVer <= 768 then yield node
    ]



type VerAndPacket = { Version: int; JsonPacket: JsonNode }

let toTestPacket (node: ProtocolNode, packetName: String) : VerAndPacket =
    try
        let a = node.JsonProtocol["play"]["toServer"]["types"][packetName][1]

        { Version = node.ProtocolVersion
          JsonPacket = a }
    with :? Exception ->
        { Version = node.ProtocolVersion
          JsonPacket = JsonValue.Create("empty") }


let allServerboundPackets = HashSet<string>()

let protocols = dataPaths["pc"].AsObject() |> toProtocols

//protocols |> Seq.iter (fun x -> printfn "%d" x.ProtocolVersion)

protocols
|> Seq.map (fun x -> x.JsonProtocol["play"]["toServer"]["types"])
|> Seq.iter (fun x ->
    x.AsObject()
    |> Seq.where (_.Key.StartsWith("packet_"))
    |> Seq.iter (fun g -> allServerboundPackets.Add(g.Key) |> ignore))



Directory.CreateDirectory("packets") |> ignore

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
    File.WriteAllText(Path.Combine("packets", $"{packet}.json"), resultJson)
