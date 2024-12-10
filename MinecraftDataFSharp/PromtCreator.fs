module MinecraftDataFSharp.PromtCreator

open System.IO

type PromtInfo =
    { PacketId: string
      PacketName: string
      Structure: string }

let private basePromt = File.ReadAllText "BasePromt.txt"

let createPromt (info: PromtInfo) : string =
    basePromt
        .Replace("%packet_name%", info.PacketName)
        .Replace("%packet_json%", info.Structure)
        .Replace("%packet_id%", info.PacketId)
