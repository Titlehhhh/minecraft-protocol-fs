namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ArgumentSignature =

    let argumentSignature =
        record "ArgumentSignature" (Since 766) [
            col "argumentName" Str
            col "signature"    (FixedBytes 256)
        ]
