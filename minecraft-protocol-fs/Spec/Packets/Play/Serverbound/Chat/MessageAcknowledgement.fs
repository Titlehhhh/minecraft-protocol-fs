namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module MessageAcknowledgement =

    let messageAcknowledgement =
        packet "MessageAcknowledgementPacket" Play Serverbound (Since 760) {
            api
                [
                    field "PreviousMessages"    (TArray(TNamed "PreviousMessage"))      (Between(760, 760))
                    field "LastRejectedMessage" (TOption(TNamed "LastRejectedMessage")) (Between(760, 760))
                    field "Count"               TInt                                   (Since 761)
                ]

            wire
                (Between(760, 760))
                [
                    read "previousMessages"    (Array(Named "PreviousMessage", VarIntCount)) "PreviousMessages"
                    read "lastRejectedMessage" (Option(Named "LastRejectedMessage"))         "LastRejectedMessage"
                ]

            wire (Since 761) [ read "count" VarInt "Count" ]
        }
