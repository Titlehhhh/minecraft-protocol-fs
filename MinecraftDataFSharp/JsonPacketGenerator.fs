module MinecraftDataFSharp.JsonPacketGenerator

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open MinecraftDataFSharp.Models

let findRanges (input: VerAndPacket array) =
    let jsonObj = JsonObject();
    input
    |> List.ofArray
    |> List.fold (fun acc current ->
        match acc with
        | [] -> [{ Version = current.Version; JsonStructure = current.JsonStructure }, current.Version, current.Version]
        | (lastObj, startVer, _) :: rest when 
            JsonNode.DeepEquals(lastObj.JsonStructure, current.JsonStructure) ->            
            (lastObj, startVer, current.Version) :: rest
        | _ ->
            ({ Version = current.Version; JsonStructure = current.JsonStructure }, current.Version, current.Version) :: acc
    ) []
    |> List.rev
    |> List.iter (fun (obj, startVer, endVer) ->
        let key =
            if startVer = endVer then
                startVer.ToString()
            else
                sprintf "%d-%d" startVer endVer
        let value = obj.JsonStructure.DeepClone()
        jsonObj.Add(key,value))
    jsonObj
    
    

let generatePackets (protocols: ProtocolVersionEntry list) (side: string) : PacketMetadata list =
    let packetNames = HashSet<string>()

    protocols
    |> Seq.map (fun x -> x.JsonProtocol["play"][side]["types"])
    |> Seq.iter (fun x ->
        x.AsObject()
        |> Seq.where _.Key.StartsWith("packet_")
        |> Seq.iter (fun g -> packetNames.Add(g.Key) |> ignore))

    
    let extractPacket (node: ProtocolVersionEntry, packetName: string) : VerAndPacket =
        try
            let packet = node.JsonProtocol["play"][side]["types"][packetName][1]
            { Version = node.ProtocolVersion
              JsonStructure = packet }
        with
        | _ -> { Version = node.ProtocolVersion
                 JsonStructure = JsonValue.Create("empty") }
        
    let packets = List<PacketMetadata>()
    
    for packet in packetNames do

        let listPackets =
            protocols |> Seq.map (fun t -> extractPacket (t, packet)) |> Seq.toArray
        
        let obj = findRanges listPackets
        //Thread.Sleep(-1)
        let resultJson = obj.ToJsonString(JsonSerializerOptions(WriteIndented = true))

        packets.Add(
            { PacketId = packet
              PacketName = packet
              Structure = resultJson }
        )
    packets |> Seq.toList