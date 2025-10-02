namespace PacketGenerator.Protocol

open System.Text.Json
open System.Text.Json.Nodes
open PacketGenerator.Core
open Protodef
open PacketGenerator.Extensions

type VersionInterval =
    | Single of int
    | Range of int * int

    override this.ToString() =
        match this with
        | Single v -> v.ToString()
        | Range(v1, v2) -> $"{v1}-{v2}"

type TypeTimeline =
    | Absent of VersionInterval
    | Present of VersionInterval * ProtodefType

type TypeFormatHistory =
    { Name: string
      Timeline: TypeTimeline list }
    
    member this.ToJson() =
        let serializeType (t: ProtodefType) =
            JsonSerializer.SerializeToNode(t, ProtodefType.DefaultJsonOptions)        
        let obj = JsonObject()
        obj.Add("name", JsonValue.Create(this.Name))
        let timeObj = JsonObject()
        for timeline in this.Timeline do
            match timeline with
            | Absent interval -> timeObj.Add(interval.ToString(), JsonValue.Create("absent"))
            | Present(interval, t) -> timeObj.Add(interval.ToString(), serializeType t)
        obj.Add("timeline", timeObj)
        obj
    

module TypeDiffHistory =
    let private fromRange (startVersion: int) (endVersion: int) : VersionInterval =
        if startVersion = endVersion then
            Single(startVersion)
        else
            Range(startVersion, endVersion)

    type private Range =
        { StartVersion: int
          EndVersion: int
          Structure: ProtodefType option }

        member this.Interval = fromRange this.StartVersion this.EndVersion

    type private ProtocolVersionedType =
        { Version: int
          Structure: ProtodefType option }

    let private toTimeline (r: Range seq) : TypeTimeline list =
        r
        |> Seq.map (fun x ->
            match x.Structure with
            | Some t -> Present(x.Interval, t)
            | None -> Absent(x.Interval))
        |> Seq.toList

    let diffForType (map: ProtocolMap) (name: string) : TypeFormatHistory =
        let comparer = PascalizeStringComparer.Instance
        let findedTypes =
            map.Protocols
            |> Seq.map (fun p ->
                { Version = p.Key
                  Structure = p.Value.Protocol.TryFindTypeOption(name, comparer) })

        let equalTwoTypes (t1: ProtodefType option) (t2: ProtodefType option) =
            match t1, t2 with
            | Some t1, Some t2 -> t1.Equals(t2)
            | None, None -> true
            | _ -> false

        let timeline =
            findedTypes
            |> Seq.toList
            |> List.fold
                (fun acc current ->
                    match acc with
                    | [] ->
                        [ { StartVersion = current.Version
                            EndVersion = current.Version
                            Structure = current.Structure } ]
                    | last :: tail when equalTwoTypes last.Structure current.Structure ->
                        { last with
                            EndVersion = current.Version }
                        :: tail
                    | _ ->
                        { StartVersion = current.Version
                          EndVersion = current.Version
                          Structure = current.Structure }
                        :: acc)
                []
            |> List.rev
            |> toTimeline

        { Name = name; Timeline = timeline }
