namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module AcknowledgePlayerDigging =

    let acknowledgePlayerDigging =
        packet "AcknowledgePlayerDiggingPacket" Play Clientbound All {
            api [
                field "Location"   (TNamed "Position") (Until 758)
                field "Block"      TInt                (Until 758)
                field "Status"     TInt                (Until 758)
                field "Successful" TBool               (Until 758)
                field "SequenceId" TInt                (Since 759)
            ]

            wire (Until 758) [
                read "location"   (Named "Position") "Location"
                read "block"      VarInt             "Block"
                read "status"     VarInt             "Status"
                read "successful" Bool               "Successful"
            ]

            wire (Since 759) [
                read "sequenceId" VarInt "SequenceId"
            ]
        }
