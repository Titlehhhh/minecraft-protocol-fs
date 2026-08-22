namespace McProtocol.Spec

open System.Reflection
open FSharp.Reflection
open McProtocol.Dsl

/// The assembled protocol. Every spec (`namedType` / `unionType` / `packet` binding) in this
/// assembly is discovered by reflection, so a new type/union/packet needs no registration here —
/// just drop a file under Spec/. No shared-file edit per type = safe for parallel authoring.
[<AutoOpen>]
module Protocol =

    /// All module-level values of type 'T declared anywhere in this assembly.
    let private collect<'T> () : 'T list =
        [ for t in Assembly.GetExecutingAssembly().GetTypes() do
              if FSharpType.IsModule t then
                  for p in t.GetProperties(BindingFlags.Static ||| BindingFlags.Public) do
                      if p.PropertyType = typeof<'T> && p.GetIndexParameters().Length = 0 then
                          match (try Some(p.GetValue null :?> 'T) with _ -> None) with
                          | Some v -> yield v
                          | None -> () ]

    let protocol : ProtocolSpec =
        { Types    = collect<NamedTypeSpec> () |> List.sortBy (fun t -> t.Name)
          Unions   = collect<UnionTypeSpec> () |> List.sortBy (fun u -> u.Name)
          Bitflags = collect<BitflagsSpec> ()  |> List.sortBy (fun b -> b.Name)
          Enums    = collect<EnumSpec> ()      |> List.sortBy (fun e -> e.Name)
          Packets  = collect<PacketSpec> ()    |> List.sortBy (fun p -> p.ClassName) }
