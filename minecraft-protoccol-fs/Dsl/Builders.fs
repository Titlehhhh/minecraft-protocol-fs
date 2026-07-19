namespace McProtocol.Dsl

/// Computation-expression builders and their constructor functions.
/// `packet` / `namedType` / `unionType` are the entry points every spec uses.
[<AutoOpen>]
module Builders =

    type PacketBuilder(name, state, direction, since) =
        let mutable apiFields : ApiField list = []
        let mutable layouts   : WireLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f()
        member _.Combine((), f) = f()

        [<CustomOperation("api")>]
        member _.Api((), fields) =
            apiFields <- fields

        [<CustomOperation("wire")>]
        member _.Wire((), range, entries) =
            layouts <- layouts @ [{ Range = range; Entries = entries }]

        member _.Run(()) = {
            ClassName = name
            State     = state
            Direction = direction
            Since     = since
            ApiFields = apiFields
            Layouts   = layouts
        }

    type NamedTypeBuilder(name) =
        let mutable apiFields : ApiField list = []
        let mutable layouts   : WireLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f()
        member _.Combine((), f) = f()

        [<CustomOperation("api")>]
        member _.Api((), fields) =
            apiFields <- fields

        [<CustomOperation("wire")>]
        member _.Wire((), range, entries) =
            layouts <- layouts @ [{ Range = range; Entries = entries }]

        member _.Run(()) = {
            Name      = name
            ApiFields = apiFields
            Layouts   = layouts
        }

    type UnionTypeBuilder(name) =
        let mutable layouts : UnionLayout list = []

        member _.Yield(()) = ()
        member _.Zero() = ()
        member _.Delay(f) = f()
        member _.Combine((), f) = f()

        [<CustomOperation("cases")>]
        member _.Cases((), range, arms) =
            layouts <- layouts @ [{ Range = range; Arms = arms }]

        member _.Run(()) = {
            Name    = name
            Layouts = layouts
        }


    // ===== CONSTRUCTORS =====

    let packet name state direction since =
        PacketBuilder(name, state, direction, since)

    let namedType name =
        NamedTypeBuilder(name)

    let unionType name =
        UnionTypeBuilder(name)
