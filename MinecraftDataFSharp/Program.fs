open System
open System.Collections.Generic
open System.Diagnostics
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
open Protodef



let protocols = MinecraftDataParser.getPcProtocols

//ProtocolTypeMapper.generateVersionedTypeMap protocols
let packets = JsonPacketGenerator.generatePackets protocols "toServer"
let filterPrimitivePackets (packet: PacketMetadata) = Extensions.IsPrimitive packet.Structure
let packetFolders = [ "primitive"; "complex" ]

packetFolders
|> Seq.iter (fun folder ->
    let folderPath = Path.Combine("packets", folder)

    if Directory.Exists(folderPath) then
        Directory.EnumerateFiles(folderPath) |> Seq.iter File.Delete

    Directory.CreateDirectory(folderPath) |> ignore)

packets
|> Seq.iter (fun packet ->
    let folder =
        if filterPrimitivePackets packet then
            "primitive"
        else
            "complex"
    let filePath = Path.Combine("packets", folder, $"{packet.PacketName}.json")
    File.WriteAllText(filePath, packet.Structure.ToJsonString(JsonSerializerOptions(WriteIndented = true))))

let primitivePackets = packets |> Seq.filter filterPrimitivePackets |> List.ofSeq

CodeGenerator.generatePrimitive primitivePackets |> ignore

exit 0
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
