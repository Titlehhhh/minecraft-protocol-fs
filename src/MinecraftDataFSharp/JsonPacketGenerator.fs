module MinecraftDataFSharp.JsonPacketGenerator

open System
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open MinecraftDataFSharp.Models

let findRanges (input: VerAndPacket array) =
    let jsonObj = JsonObject()
    input
    |> List.ofArray
    |> List.fold
        (fun acc current ->
            match acc with
            | [] ->
                [ { Version = current.Version
                    JsonStructure = current.JsonStructure },
                  current.Version,
                  current.Version ]
            | (lastObj, startVer, _) :: tail when JsonNode.DeepEquals(lastObj.JsonStructure, current.JsonStructure) ->
                (lastObj, startVer, current.Version) :: tail
            | _ ->
                ({ Version = current.Version
                   JsonStructure = current.JsonStructure },
                 current.Version,
                 current.Version)
                :: acc)
        []
    |> List.rev
    |> List.iter (fun (obj, startVer, endVer) ->
        let key =
            if startVer = endVer then
                startVer.ToString()
            else
                sprintf "%d-%d" startVer endVer

        let value = obj.JsonStructure.DeepClone()
        jsonObj.Add(key, value))

    jsonObj



let generatePackets (protocols: ProtocolVersionEntry list) (side: string) (state: string) : PacketMetadata list =
    let packetNames =
        protocols
        |> Seq.map (fun x ->
            try
                Some(x.JsonProtocol.AsObject()[state][side]["types"])
            with _ ->
                None)
        |> Seq.choose id
        |> Seq.map _.AsObject()
        |> Seq.map (fun x -> x |> Seq.map(_.Key))
        |> Seq.concat
        |> HashSet
        
        


    let extractPacket (node: ProtocolVersionEntry, packetName: string) : VerAndPacket =
        try
            let packet = node.JsonProtocol[state][side]["types"][packetName][1]

            { Version = node.ProtocolVersion
              JsonStructure = packet }
        with _ ->
            { Version = node.ProtocolVersion
              JsonStructure = JsonValue.Create("empty") }

    let packets = List<PacketMetadata>()

    for packet in packetNames do

        let listPackets =
            protocols |> Seq.map (fun t -> extractPacket (t, packet)) |> Seq.toArray

        let obj = findRanges listPackets

        packets.Add(
            { PacketName = packet
              Structure = obj.DeepClone().AsObject() }
        )

    packets |> Seq.toList
