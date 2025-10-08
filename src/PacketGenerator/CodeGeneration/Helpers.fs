module PacketGenerator.CodeGeneration.Helpers

open System.Collections.Generic
open System.Linq
open PacketGenerator.Core
open PacketGenerator.Extensions
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
    let comparer = ProtodefContainerField.ClrNameComparer

    (match nonOption with
     | [] -> HashSet<ProtodefContainerField>(comparer)
     | first :: rest ->
         let acc = HashSet(first.Fields, comparer)
         rest |> List.iter (fun x -> acc.IntersectWith(x.Fields))
         acc)

let getDiff (common: HashSet<ProtodefContainerField>) (fields: seq<ProtodefContainerField>) =
    fields |> Seq.filter (common.Contains >> not)

let toField (isCommon: bool) (contField: ProtodefContainerField)  : FieldDefinition =
    
    let kind = 
        match contField.Type with
        | Protodef f -> f
    
    { Name = contField.Name |> Naming.property
      ClrType = contField.ClrTypeOption
      OriginalType = contField.Type
      Kind = kind
      IsCommon = isCommon }

let toSpec (h: TypeStructureHistory) (name: string) : ClassSpec =
    let containers = h |> Seq.map toOptionContainer |> Seq.toArray
    let commonFields = intersect containers

    let versioned =
        h
        |> Seq.choose (fun x ->
            match toOptionContainer x with
            | Some cont ->
                let diff = getDiff commonFields cont.Fields |> Seq.map (toField false) |> Seq.toList
                Some(x.Interval, diff)
            | None -> None)
        |> Seq.filter (snd >> List.isEmpty >> not)
        |> Seq.toList
    
    
    let contDefList (c: ProtodefContainer) =
        let comm (f: ProtodefContainerField) =
            (commonFields.Contains(f), f) ||> toField
        
        c.Fields |> Seq.map comm |> Seq.toList

    let ordered =
        h
        |> Seq.map (fun x -> (x.Interval, toOptionContainer x))
        |> Seq.filter (snd >> Option.isSome)
        |> Seq.map (fun (i, cont) -> (i, cont |> Option.get |> contDefList))
        |> Seq.toList


    let commonFields = commonFields |> Seq.map (toField true) |> Seq.toList

    let meta =
        { Name = name |> Naming.className
          Aliases = []
          CanonicalName = Some ""
          Direction = Some ""
          State = Some "" }

    { Meta = meta
      CommonFields = commonFields
      Versioned = versioned
      Ordered = ordered }
