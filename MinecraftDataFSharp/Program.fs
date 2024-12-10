open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Net.Http
open System.Net.Http.Json
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open FSharp.Control
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core
open MinecraftDataFSharp
open MinecraftDataFSharp.MinecraftDataParser
open MinecraftDataFSharp.Models
open MinecraftDataFSharp.PromtCreator
open OllamaSharp
open OllamaSharp.Models
open Semver
open PromtCreator


let protocols = MinecraftDataParser.getPcProtocols

ProtocolTypeMapper.generateVersionedTypeMap protocols


type VerAndPacket = { Version: int; JsonPacket: JsonNode }

let side = "toServer"

let toTestPacket (node: ProtocolVersionEntry, packetName: string) : VerAndPacket =
    try
        let a = node.JsonProtocol["play"][side]["types"][packetName][1]

        { Version = node.ProtocolVersion
          JsonPacket = a }
    with :? Exception ->
        { Version = node.ProtocolVersion
          JsonPacket = JsonValue.Create("empty") }


let allServerboundPackets = HashSet<string>()



protocols
|> Seq.map (fun x -> x.JsonProtocol["play"][side]["types"])
|> Seq.iter (fun x ->
    x.AsObject()
    |> Seq.where _.Key.StartsWith("packet_")
    |> Seq.iter (fun g -> allServerboundPackets.Add(g.Key) |> ignore))





//List PromtInfo
let promts = List<PromtInfo>()

Directory.CreateDirectory("toServerPackets") |> ignore

// print all serverbound packets
printfn "Print all serverbound packets"
for packet in allServerboundPackets do
    printfn "%s" packet

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
    let filePath = Path.Combine("toServerPackets", $"{packet}_0.json")
    File.WriteAllText(filePath, resultJson)
    promts.Add(
        { PacketId = packet
          PacketName = packet
          Structure = resultJson }
    )



// Get 10 first
let firt10Promts = promts |> Seq.take 10 |> Seq.map (fun x -> createPromt x)

let systemMessage = "You C# code generator for Minecraft protocol library"
Directory.CreateDirectory("packets") |> ignore
printfn "Start generation"

promts
|> Seq.take 10
|> Seq.map (fun info ->
    let promt = createPromt info
    //printfn "%s" promt
    async {
        
        let! result = LLMService.Predict(promt, systemMessage)
        
        
        let filePath = Path.Combine("packets", $"{info.PacketName}.cs")

        match result with
        | Some x -> return! File.WriteAllTextAsync(filePath, x) |> Async.AwaitTask
        | None -> return ()
    })
|> Async.Parallel
|> Async.RunSynchronously
|> ignore
