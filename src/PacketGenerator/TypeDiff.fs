module TypeDiff

open System
open System.Collections.Generic
open PacketGenerator.Core
open PacketGenerator.Types
open Protodef
open Test


let private createRange ver : VersionRange =
    { StartVersion = ver; EndVersion = ver }

let equalTwoTypes (t1: ProtodefType option) (t2: ProtodefType option) =
    match t1, t2 with
    | Some t1, Some t2 -> t1.Equals(t2)
    | None, None -> true
    | _ -> false

let find (path: string) (map: ProtocolMap) : TypeStructureHistory =
    let comparer = PascalizeStringComparer.Instance

    let types = map.findTypesByPath path

    

    types
    |> Seq.fold
        (fun acc current ->
            match acc with
            | [] ->
                [ { Interval = createRange current.Version
                    Structure = current.Structure } ]
            | last :: tail when equalTwoTypes last.Structure current.Structure ->
                let newInterval =
                    { last.Interval with
                        EndVersion = current.Version }

                { last with Interval = newInterval } :: tail
            | _ ->
                { Interval = createRange current.Version
                  Structure = current.Structure }
                :: acc)
        []
    |> List.rev

let canMerge (h1: TypeStructureHistory) (h2: TypeStructureHistory) =
    let toMap h =
        h |> Seq.map (fun x -> x.Interval, x.Structure) |> Map.ofSeq

    let m1 = toMap h1
    let m2 = toMap h2

    let m1Keys = m1 |> Map.keys |> Set
    let m2Keys = m2 |> Map.keys |> Set
    
    let intervals = Set.union m1Keys m2Keys

    intervals
    |> Seq.forall (fun i ->
        match Map.tryFind i m1, Map.tryFind i m2 with
        | None, None -> false        // обе пустые → бесполезно
        | Some s1, None -> true      // h1 даёт структуру
        | None, Some s2 -> true      // h2 даёт структуру
        | Some _, Some _ -> false    // конфликт
    )
        
