module PacketGenerator.CodeGeneration.Helpers

open System.Collections.Generic
open System.Linq
open PacketGenerator.Core
open PacketGenerator.Types
open PacketGenerator.Utils
open PacketGenerator.Protodef
open Protodef
open Protodef.Enumerable

let toOptionContainer (x: TypeStructureRecord) =
    match x.Structure |> Option.toObj with
    | :? ProtodefContainer as cont -> Some cont
    | _ -> None
    
let intersect (sq: ProtodefContainer option seq) =
    let nonOption = sq |> Seq.choose id |> Seq.toList
    (match nonOption with
    | [] -> HashSet<ProtodefContainerField>(ProtodefContainerField.ClrNameComparer)
    | first::rest ->
        let acc = HashSet(first.Fields, ProtodefContainerField.ClrNameComparer)
        rest |> List.iter (fun x -> acc.IntersectWith(x.Fields))
        acc) |> Seq.toArray
    

let toSpec (h: TypeStructureHistory) : PacketSpec =    
    let a = h |> Seq.map toOptionContainer
    let a = a |> Seq.toArray
    let intersected = a |> intersect


    failwith ""