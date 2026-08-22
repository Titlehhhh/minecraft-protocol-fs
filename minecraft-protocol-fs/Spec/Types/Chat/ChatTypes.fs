namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatTypes =

    let chatTypes =
        record "ChatTypes" (Since 766) [
            col "chat"      (Named "ChatType")
            col "narration" (Named "ChatType")
        ]
