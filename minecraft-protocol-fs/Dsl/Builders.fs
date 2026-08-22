namespace McProtocol.Dsl

/// Computation-expression builders and their constructor functions.
/// `packet` / `namedType` / `unionType` are the entry points every spec uses.
[<AutoOpen>]
module Builders =

    type PacketBuilder(name, state, direction, since) =
        let mutable apiFields: ApiField list = []
        let mutable layouts: WireLayout list = []
        let mutable protoName = None

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f ()
        member _.Combine((), f) = f ()

        [<CustomOperation("api")>]
        member _.Api((), fields) = apiFields <- fields

        [<CustomOperation("wire")>]
        member _.Wire((), range, entries) =
            layouts <- layouts @ [ { Range = range; Entries = entries } ]

        [<CustomOperation("protoId")>]
        member _.ProtoId((), name) = protoName <- Some name

        member _.Run(()) =
            {
                ClassName = name
                State = state
                Direction = direction
                Since = since
                ApiFields = apiFields
                Layouts = layouts
                ProtoName = protoName
                Ids = []
            }

    type NamedTypeBuilder(name) =
        let mutable apiFields: ApiField list = []
        let mutable layouts: WireLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f ()
        member _.Combine((), f) = f ()

        [<CustomOperation("api")>]
        member _.Api((), fields) = apiFields <- fields

        [<CustomOperation("wire")>]
        member _.Wire((), range, entries) =
            layouts <- layouts @ [ { Range = range; Entries = entries } ]

        member _.Run(()) =
            {
                Name = name
                ApiFields = apiFields
                Layouts = layouts
            }

    type UnionTypeBuilder(name) =
        let mutable layouts: UnionLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f ()
        member _.Combine((), f) = f ()

        [<CustomOperation("cases")>]
        member _.Cases((), range, arms) =
            layouts <- layouts @ [ { Range = range; Arms = arms } ]

        member _.Run(()) : UnionTypeSpec = { Name = name; Layouts = layouts }


    type BitflagsBuilder(name) =
        let mutable layouts: BitflagsLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f ()
        member _.Combine((), f) = f ()

        [<CustomOperation("layout")>]
        member _.Layout((), range, backing, flags) =
            layouts <-
                layouts
                @ [
                    {
                        Range = range
                        Backing = backing
                        Flags = flags
                    }
                ]

        member _.Run(()) : BitflagsSpec = { Name = name; Layouts = layouts }


    type EnumTypeBuilder(name) =
        let mutable layouts: EnumLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f ()
        member _.Combine((), f) = f ()

        [<CustomOperation("values")>]
        member _.Values((), range, backing, values) =
            layouts <-
                layouts
                @ [
                    {
                        Range = range
                        Backing = backing
                        Values = values
                    }
                ]

        member _.Run(()) : EnumSpec = { Name = name; Layouts = layouts }


    // ===== CONSTRUCTORS =====

    let packet name state direction since =
        PacketBuilder(name, state, direction, since)

    let namedType name = NamedTypeBuilder(name)

    let unionType name = UnionTypeBuilder(name)

    let bitflags name = BitflagsBuilder(name)

    let enumType name = EnumTypeBuilder(name)
