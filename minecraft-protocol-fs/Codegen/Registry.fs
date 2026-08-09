namespace McProtocol.Codegen

open McProtocol.Dsl

/// The ordinal catalog: every packet of one (state, direction), sorted by manifest key.
/// The ordinal — a packet's index in that sorted list — is the hot-path currency of the
/// generated registry and dispatcher (jump tables, subscription slots). The key is stable,
/// therefore the ordinal is stable across builds, unlike compile order.
module Registry =

    type CatalogEntry =
        {
            Spec: PacketSpec
            Key: string
            Ordinal: int
        }

    /// Catalog every packet: group by (State, Direction), sort each group by key,
    /// index within the group. Returns entries in (state, direction, ordinal) order.
    let catalog (packets: PacketSpec list) : CatalogEntry list =
        packets
        |> List.groupBy (fun p -> p.State, p.Direction)
        |> List.sortBy fst
        |> List.collect (fun (_, group) ->
            group
            |> List.sortBy PacketIds.key
            |> List.mapi (fun i p ->
                {
                    Spec = p
                    Key = PacketIds.key p
                    Ordinal = i
                }))

    /// One (state, direction) slice of the catalog, ordinal-ordered.
    let slice (state: ProtocolState) (direction: Direction) (entries: CatalogEntry list) : CatalogEntry list =
        entries
        |> List.filter (fun e -> e.Spec.State = state && e.Spec.Direction = direction)
        |> List.sortBy (fun e -> e.Ordinal)
