namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginSignature =

    let loginSignature =
        record "LoginSignature" (Between(759, 760)) [
            col "timestamp" I64
            col "publicKey" ByteArray
            col "signature" ByteArray
        ]
