namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ChatSuggestions =

    let chatSuggestions =
        packet "ChatSuggestionsPacket" Play Clientbound (Since 760) {
            api [
                field "Action"  TInt             All
                field "Entries" (TArray TString) All
            ]

            wire (Since 760) [
                read "action"  VarInt                    "Action"
                read "entries" (Array(Str, VarIntCount)) "Entries"
            ]
        }
