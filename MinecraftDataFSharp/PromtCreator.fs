module MinecraftDataFSharp.PromtCreator

open System.IO
open MinecraftDataFSharp.Models



let private basePromt = File.ReadAllText "BasePromt.txt"

let createPromt (packet: PacketMetadata) : string =
    basePromt
        .Replace("%packet_name%", packet.PacketName)
        .Replace("%packet_json%", packet.Structure)
        .Replace("%packet_id%", packet.PacketId)
