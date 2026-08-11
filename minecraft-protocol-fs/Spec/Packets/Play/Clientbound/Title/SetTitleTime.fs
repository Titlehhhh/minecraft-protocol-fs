namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module SetTitleTime =

    let setTitleTime =
        packet "SetTitleTimePacket" Play Clientbound (Since 755) {
            api [
                field "FadeIn"  TInt All
                field "Stay"    TInt All
                field "FadeOut" TInt All
            ]

            wire (Since 755) [
                read "fadeIn"  I32 "FadeIn"
                read "stay"    I32 "Stay"
                read "fadeOut" I32 "FadeOut"
            ]
        }
