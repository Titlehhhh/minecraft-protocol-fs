namespace PacketGenerator.History

open PacketGenerator.Core
open PacketGenerator.Types
open PacketGenerator.Utils
open PacketGenerator.Extensions



module HistoryBuilder =
    let buildForPath (path: string) (map: ProtocolMap) : TypeStructureHistory =
        let comparer = PascalizeStringComparer.Instance
        let types = map.findTypesByPath path

        types
        |> Seq.fold
            (fun acc current ->
                match acc with
                | [] ->
                    [ { Interval = VersionRange.create current.Version
                        Structure = current.Structure } ]
                | last :: tail when Util.equalTwoTypes last.Structure current.Structure ->
                    let newInterval =
                        { last.Interval with
                            EndVersion = current.Version }

                    { last with Interval = newInterval } :: tail
                | _ ->
                    { Interval = VersionRange.create current.Version
                      Structure = current.Structure }
                    :: acc)
            []
        |> List.rev

    let canMerge (h1: TypeStructureHistory) (h2: TypeStructureHistory) =
        let toMap h =
            h |> Seq.map (fun x -> x.Interval, x.Structure) |> Map.ofSeq
        
        let isEmpty (x:TypeStructureRecord) =
            x.Structure |> Option.isNone
        let isNonEmpty (x:TypeStructureRecord) =
            x.Structure |> Option.isSome
        
        let getEmptyIntervals h =
            h
            |> Seq.filter isEmpty
            |> Seq.map _.Interval
            |> Seq.map _.ToArray()
            |> Seq.concat
            |> Set
        
        let getNonEmptyIntervals h =
            h
            |> Seq.filter isNonEmpty
            |> Seq.map _.Interval
            |> Seq.map _.ToArray()
            |> Seq.concat
            |> Set
            
        
        let emptyInts1 = h1 |> getEmptyIntervals
        let emptyInts2 = h2 |> getEmptyIntervals
        
        let nonEmptyInts1 = h1 |> getNonEmptyIntervals
        let nonEmptyInts2 = h2 |> getNonEmptyIntervals
        
        let a = (emptyInts1 |> Set.intersect emptyInts2) |> Set.isEmpty
        let b = (nonEmptyInts1 |> Set.intersect nonEmptyInts2) |> Set.isEmpty
        
        a && b

    let build map path = buildForPath path map
