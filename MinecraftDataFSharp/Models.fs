module MinecraftDataFSharp.Models

open System.Text.Json.Nodes

type ProtocolVersionEntry =
    { ProtocolVersion: int
      MinVersion: string
      MaxVersion: string
      JsonProtocol: JsonNode }

type PacketMetadata =
    { PacketId: string
      PacketName: string
      Structure: JsonObject
       }

type VerAndPacket = { Version: int; JsonStructure: JsonNode }
