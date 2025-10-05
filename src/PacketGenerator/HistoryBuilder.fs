namespace PacketGenerator.History

open PacketGenerator.Core
open PacketGenerator.Types
open PacketGenerator.Utils
open PacketGenerator.Extensions



module HistoryBuilder =
    let private fold types =
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
    
    let buildForPath (path: string) (map: ProtocolMap) : TypeStructureHistory =
        let comparer = PascalizeStringComparer.Instance
        let types = map.findTypesByPath path
        
        types |> fold
    let private isEmpty (x:TypeStructureRecord) =
            x.Structure |> Option.isNone
    let private isNonEmpty (x:TypeStructureRecord) =
        x.Structure |> Option.isSome    
    let private getEmptyIntervals h =
            h
            |> Seq.filter isEmpty
            |> Seq.map _.Interval
            |> Seq.map _.ToArray()
            |> Seq.concat
            |> Set
        
    let private getNonEmptyIntervals h =
        h
        |> Seq.filter isNonEmpty
        |> Seq.map _.Interval
        |> Seq.map _.ToArray()
        |> Seq.concat
        |> Set
        
    let private toFindResult (h:TypeStructureHistory): TypeFinderResult seq =
        h
        |> Seq.collect (fun entry ->
        let arr = entry.Interval.ToArray()
        seq {
            for v in arr do
                if entry.Structure |> Option.isSome then
                    yield { Version = v; Structure = entry.Structure }
        })
    let canMerge (h1: TypeStructureHistory) (h2: TypeStructureHistory) =
        
        let emptyInts1 = h1 |> getEmptyIntervals
        let emptyInts2 = h2 |> getEmptyIntervals
        
        let nonEmptyInts1 = h1 |> getNonEmptyIntervals
        let nonEmptyInts2 = h2 |> getNonEmptyIntervals
        
        let a = (emptyInts1 |> Set.intersect emptyInts2) |> Set.isEmpty
        let b = (nonEmptyInts1 |> Set.intersect nonEmptyInts2) |> Set.isEmpty
        
        a && b

    let build map path = buildForPath path map
    
    let merge (h1: TypeStructureHistory) (h2: TypeStructureHistory) : TypeStructureHistory =
        if canMerge h1 h2 then
            let f1 = toFindResult h1
            let f2 = toFindResult h2
            
            let all = f1 |> Seq.append f2
            let all = all |> Seq.sortBy _.Version            
            all |> fold

        else failwithf "Cant merge %A and %A" h1 h2
            
