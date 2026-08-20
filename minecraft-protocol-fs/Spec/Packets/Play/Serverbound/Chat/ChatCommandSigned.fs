namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatCommandSigned =

    let chatCommandSigned =
        packet "ChatCommandSignedPacket" Play Serverbound (Since 766) {
            api
                [
                    field "Command" TString All
                    field "Timestamp" TLong All
                    field "Salt" TLong All
                    field "ArgumentSignatures" (TArray(TNamed "ArgumentSignature")) All
                    field "MessageCount" TInt All
                    field "Acknowledged" TBytes All
                    field "Checksum" TInt (Since 770)
                ]

            wire
                (Between(766, 769))
                [
                    read "command" Str "Command"
                    read "timestamp" I64 "Timestamp"
                    read "salt" I64 "Salt"
                    read "argumentSignatures" (Array(Named "ArgumentSignature", VarIntCount)) "ArgumentSignatures"
                    read "messageCount" VarInt "MessageCount"
                    read "acknowledged" (FixedBytes 3) "Acknowledged"
                ]

            wire
                (Since 770)
                [
                    read "command" Str "Command"
                    read "timestamp" I64 "Timestamp"
                    read "salt" I64 "Salt"
                    read "argumentSignatures" (Array(Named "ArgumentSignature", VarIntCount)) "ArgumentSignatures"
                    read "messageCount" VarInt "MessageCount"
                    read "acknowledged" (FixedBytes 3) "Acknowledged"
                    read "checksum" U8 "Checksum"
                ]
        }
