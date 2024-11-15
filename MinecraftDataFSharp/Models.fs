module MinecraftDataFSharp.Models

open System.Text.Json.Nodes

type ProtocolVersionEntry =
    { ProtocolVersion: int
      MinVersion: string
      MaxVersion: string
      JsonProtocol: JsonNode }