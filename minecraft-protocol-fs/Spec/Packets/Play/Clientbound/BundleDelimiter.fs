namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module BundleDelimiter =

    let bundleDelimiter =
        packet "BundleDelimiterPacket" Play Clientbound (Since 762) {
            api []

            wire (Since 762) []
        }
