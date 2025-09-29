module MinecraftDataFSharp.MinecraftDataParser
open System.IO
open System.Text.Json.Nodes
open MinecraftDataFSharp.Models

[<Literal>]
let minecraftDataRootPath = "minecraft-data"

let private getProtocolVersion (node: JsonNode) : int =
    let verPath = (node.AsObject()["version"]).ToString()

    let version =
        (Path.Combine(minecraftDataRootPath, "data", verPath, "version.json")
         |> File.ReadAllText
         |> JsonObject.Parse)
            .AsObject()

    (version["version"]).ToString() |> int

let private getJsonProtocol (node: JsonNode) : JsonNode =
    let protoPath = (node.AsObject()["protocol"]).ToString()

    Path.Combine(minecraftDataRootPath, "data", protoPath, "protocol.json")
    |> File.ReadAllText
    |> JsonObject.Parse

let getPcProtocols =
    let dataPaths =
        Path.Combine(minecraftDataRootPath, "data", "dataPaths.json")
        |> File.ReadAllText
        |> JsonObject.Parse       
    
    let dataPaths = dataPaths["pc"].AsObject()

    let grouped =
        dataPaths
        |> Seq.groupBy (fun x -> getProtocolVersion x.Value)
        |> Seq.where (fun x -> x |> fst >= Models.MinVersionProtocol)

    [ for tuple in grouped do
          let protoVer = fst tuple
          let versions = snd tuple
          let protocolNode = (versions |> Seq.head).Value |> getJsonProtocol

          let onlyVersions = versions |> Seq.map _.Key |> Seq.toArray
          let minVersion = onlyVersions |> Array.head
          let maxVersion = onlyVersions[onlyVersions.Length - 1]

          let node =
              { ProtocolVersion = protoVer
                MinVersion = minVersion
                MaxVersion = maxVersion
                JsonProtocol = protocolNode }

          if protoVer <= Models.MaxVersionProtocol then
              yield node ]
    
