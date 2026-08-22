namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LastRejectedMessage =

    let lastRejectedMessage =
        record "LastRejectedMessage" (Between(760, 760)) [
            col "sender"    Uuid
            col "signature" ByteArray
        ]
