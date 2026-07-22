namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MapColorData =

    let mapColorData =
        namedType "MapColorData" {
            api [
                field "Rows" TInt All
                field "X"    TInt All
                field "Y"    TInt All
                field "Data" TBytes All
            ]

            wire All [
                read "rows" I8        "Rows"
                read "x"    I8        "X"
                read "y"    I8        "Y"
                read "data" ByteArray "Data"
            ]
        }
