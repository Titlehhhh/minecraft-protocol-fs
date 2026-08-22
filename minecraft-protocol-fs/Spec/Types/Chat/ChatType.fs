namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatType =

    let chatType =
        record "ChatType" (Since 766) [
            col "translationKey" Str
            col "parameters"     (Array(enumOf "ChatTypeParameterType", VarIntCount))
            col "style"          AnonNbt
        ]
