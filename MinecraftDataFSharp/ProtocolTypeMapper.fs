module MinecraftDataFSharp.ProtocolTypeMapper

open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Core
open MinecraftDataFSharp.Models
open Models

type TypeMeta =
    { Version: int
      Name: string
      Structure: JsonNode }

let findRanges (input: TypeMeta array) =
    let jsonObj = JsonObject()

    input
    |> List.ofArray
    |> List.fold
        (fun acc current ->
            match acc with
            | [] ->
                [ { Version = current.Version
                    Name = current.Name
                    Structure = current.Structure },
                  current.Version,
                  current.Version ]
            | (lastObj, startVer, _) :: tail when JsonNode.DeepEquals(lastObj.Structure, current.Structure) ->
                (lastObj, startVer, current.Version) :: tail
            | _ ->
                ({ Version = current.Version
                   Name = current.Name
                   Structure = current.Structure },
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

        let value = obj.Structure.DeepClone()
        jsonObj.Add(key, value))

    jsonObj

let toTypeMeta (entry: ProtocolVersionEntry, name: string) =
    let types = entry.JsonProtocol["types"].AsObject()

    let structure =
        match types.TryGetPropertyValue(name) with
        | true, v -> v.DeepClone()
        | false, _ -> JsonValue.Create("empty")

    { Version = entry.ProtocolVersion
      Name = name
      Structure = structure }

let generateVersionedTypeMap (protocols: ProtocolVersionEntry seq) =

    let nonNative =
        protocols
        |> Seq.map (fun p -> (p.JsonProtocol["types"]).AsObject())
        |> Seq.map (fun x -> x :> KeyValuePair<string, JsonNode> seq)
        |> Seq.concat
        |> Seq.filter (fun x -> x.Value.GetValueKind() <> JsonValueKind.String)
        |> Seq.map _.Key
        |> Set

    Directory.CreateDirectory("types") |> ignore

    Directory.EnumerateFiles("types", "*.*", SearchOption.AllDirectories)
    |> Seq.iter (fun f ->
        try
            File.Delete(f)
        with _ ->
            ())

    nonNative
    |> Seq.iteri (fun i name ->
        let ranges =
            protocols
            |> Seq.map (fun x -> toTypeMeta (x, name))
            |> Seq.toArray
            |> findRanges

        let path = Path.Combine("types", name + $"_{i}.json")
        File.WriteAllText(path, ranges.ToJsonString(JsonSerializerOptions(WriteIndented = true))))
