namespace PacketGenerator.Protocol

open PacketGenerator.Core
open Protodef
open PacketGenerator.Extensions

type VersionInterval =
    | Single of int
    | Range of int * int
    
    
type TypeTimeline =
    | Absent of VersionInterval
    | Present of VersionInterval * ProtodefType
    
type TypeFormatHistory =
    { Name: string
      Timeline: TypeTimeline list }
    
module TypeDiffHistory =
    let private fromRange (startVersion: int) (endVersion: int) : VersionInterval =
        if startVersion = endVersion then
            Single(startVersion)
        else
            Range(startVersion, endVersion)
    
    type private Range =
        { StartVersion: int; EndVersion: int; Structure: ProtodefType option }
        with member this.Interval = fromRange this.StartVersion this.EndVersion
        
    type private ProtocolVersionedType = { Version: int; Structure: ProtodefType option }
    
    
    
    let private toTimeline (r: Range seq) : TypeTimeline list =
        r |> Seq.map (fun x ->
            match x.Structure with
            | Some t -> Present(x.Interval, t)
            | None -> Absent(x.Interval))
        |> Seq.toList
        
    
    let diffForType (map: ProtocolMap) (name: string) : TypeFormatHistory =        
        let findedTypes = map.Protocols |> Seq.map (fun p -> {
            Version = p.Key
            Structure = p.Value.Protocol.TryFindTypeOption(name)
        })
        
        let equalTwoTypes (t1: ProtodefType option) (t2: ProtodefType option) =
            match t1, t2 with
            | Some t1, Some t2 -> t1.Equals(t2)
            | None, None -> true
            | _ -> false
        let timeline = findedTypes
                       |> Seq.toList
                       |> List.fold (fun acc current ->
                            match acc with
                            | [] ->
                                [{ StartVersion = current.Version
                                   EndVersion = current.Version
                                   Structure = current.Structure }]
                            | last :: tail ->
                                if equalTwoTypes last.Structure current.Structure then
                                    { last with EndVersion = current.Version } :: tail
                                else
                                    { StartVersion = current.Version
                                      EndVersion = current.Version
                                      Structure = current.Structure } :: acc
                        ) []
                        |> List.rev
                        |> toTimeline
        { Name = name; Timeline = timeline }