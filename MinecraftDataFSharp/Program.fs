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
open MinecraftDataFSharp.Models
open MinecraftDataFSharp.PromtCreator


let protocols = MinecraftDataParser.getPcProtocols

ProtocolTypeMapper.generateVersionedTypeMap protocols


let packets = JsonPacketGenerator.generatePackets protocols "toServer"



Directory.CreateDirectory("toServer") |> ignore

packets
|> Seq.iter (fun x ->
    let filePath = Path.Combine("toServer", $"{x.PacketName}.json")
    File.WriteAllText(filePath, x.Structure))
printfn "Stop"
exit 0
// Get 10 first
let firt10Packets = packets |> Seq.take 10 |> Seq.map (fun x -> createPromt x)

let systemMessage = "You C# code generator for Minecraft protocol library"
Directory.CreateDirectory("packets") |> ignore
printfn "Start generation"

packets
|> Seq.take 10
|> Seq.map (fun info ->
    let promt = createPromt info

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
