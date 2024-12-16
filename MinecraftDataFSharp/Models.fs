module MinecraftDataFSharp.Models

open System.Collections.Generic
open System.Text.Json.Nodes
open Protodef.Enumerable

type ProtocolVersionEntry =
    { ProtocolVersion: int
      MinVersion: string
      MaxVersion: string
      JsonProtocol: JsonNode }

type PacketMetadata =
    { PacketName: string
      Structure: JsonObject }

type VerAndPacket =
    { Version: int
      JsonStructure: JsonNode }
    
type VersionRange =
    { MinVersion: int
      MaxVersion: int }

    static member Parse(s: string) =
        let parts = s.Split('-')

        if parts.Length = 1 then
            { MinVersion = int parts.[0]
              MaxVersion = int parts.[0] }
        elif parts.Length = 2 then
            { MinVersion = int parts.[0]
              MaxVersion = int parts.[1] }
        else
            invalidArg "s" "Invalid version range format"

    override this.ToString() =
        if this.MinVersion = this.MaxVersion then
            this.MinVersion.ToString()
        else
            sprintf "%d-%d" this.MinVersion this.MaxVersion


type Packet =
    { PacketName: string
      Structure: Dictionary<VersionRange, ProtodefContainer>
      EmptyRanges: VersionRange list }

    member this.IsFullPacket = this.EmptyRanges.IsEmpty
