open System.IO
open System.Text.Json
open Microsoft.FSharp.Collections
open Microsoft.FSharp.Core
open MinecraftDataFSharp
open MinecraftDataFSharp.CodeGeneration
open MinecraftDataFSharp.Models
open Protodef



let protocols = MinecraftDataParser.getPcProtocols

if Directory.Exists("packets") then
        Directory.EnumerateFiles("packets", "*.*", SearchOption.AllDirectories)
        |> Seq.iter (fun filePath ->
            try
                File.Delete(filePath)
            with _ ->
                ())

let generate (side: string) =
    let packets = JsonPacketGenerator.generatePackets protocols side

    let filterPrimitivePackets (packet: PacketMetadata) =
        Extensions.IsPrimitive packet.Structure
        
    
    let packetFolders = [ "primitive"; "complex" ]
    packetFolders |> Seq.iter (fun folder ->
        let dirPath = Path.Combine("packets", side, folder)
        Directory.CreateDirectory(dirPath) |> ignore)


    packets
    |> Seq.iter (fun packet ->
        let folder =
            if filterPrimitivePackets packet then
                "primitive"
            else
                "complex"

        let filePath = Path.Combine("packets", side, folder, $"{packet.PacketName}.json")
        File.WriteAllText(filePath, packet.Structure.ToJsonString(JsonSerializerOptions(WriteIndented = true))))

    let primitivePackets = packets |> Seq.filter filterPrimitivePackets |> List.ofSeq
    if side = "toServer" then
        CodeGeneratorWrite.generatePrimitive (primitivePackets, side) |> ignore
    else
        CodeGeneratorRead.generatePrimitive (primitivePackets, side) |> ignore


generate "toServer"
generate "toClient"

// let firt10Packets = packets |> Seq.take 10 |> Seq.map (fun x -> createPromt x)
//
// let systemMessage = "You C# code generator for Minecraft protocol library"
// Directory.CreateDirectory("packets") |> ignore
// printfn "Start generation"
//
// packets
// |> Seq.take 10
// |> Seq.map (fun info ->
//     let promt = createPromt info
//
//     async {
//         let! result = LLMService.Predict(promt, systemMessage)
//         let filePath = Path.Combine("packets", $"{info.PacketName}.cs")
//
//         match result with
//         | Some x -> return! File.WriteAllTextAsync(filePath, x) |> Async.AwaitTask
//         | None -> return ()
//     })
// |> Async.Parallel
// |> Async.RunSynchronously
// |> ignore
