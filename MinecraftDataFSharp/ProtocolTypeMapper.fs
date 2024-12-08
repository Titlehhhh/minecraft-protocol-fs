module MinecraftDataFSharp.ProtocolTypeMapper
open System.Collections.Generic
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Core
open MinecraftDataFSharp.Models
open Models

let generateVersionedTypeMap(protocols: ProtocolVersionEntry seq) =
    let allTypes = HashSet()
    
    let jsons = protocols |> Seq.map(_.JsonProtocol["types"].AsObject())
    for proto in jsons do
        proto
        |> Seq.where(fun x-> x.Value.ToString() <> "native")
        |> Seq.iter (fun x-> x.Key |> allTypes.Add |> ignore)
    
    let protocols = protocols |> Seq.toArray    
    
    let getType(json:JsonNode,typeName:string) : JsonNode =
        match json.AsObject().TryGetPropertyValue(typeName) with
            | true, v -> v.DeepClone()
            | _ -> JsonValue.Create("empty")
    
    let verRangeToStr f t = if f=t then $"{f}" else $"{f}-{t}"
    Directory.CreateDirectory("types") |> ignore
    for protoType in allTypes do
        let mutable current = protocols[0]    
        let verTypesDict: JsonObject = JsonObject()
        for i=1 to protocols.Length-1 do
            let proto = protocols[i]
            let typeFirst = getType(current.JsonProtocol["types"], protoType)
            let typeSec = getType(proto.JsonProtocol["types"], protoType)
            if not (JsonObject.DeepEquals(typeFirst, typeSec)) then
                let fromVer = current.ProtocolVersion
                let toVer = protocols[i-1].ProtocolVersion
                let key = verRangeToStr fromVer toVer
                verTypesDict.Add(key, typeFirst) 
                current <- proto
        
        let fromVer = current.ProtocolVersion
        let toVer = (protocols |> Array.last).ProtocolVersion
        let key = verRangeToStr fromVer toVer
        verTypesDict.Add(key, getType(current.JsonProtocol["types"], protoType))
        
        let path = Path.Combine("types",$"{protoType}.json")
        
        let json =verTypesDict.ToJsonString(JsonSerializerOptions(WriteIndented = true))
        File.WriteAllText(path,json)