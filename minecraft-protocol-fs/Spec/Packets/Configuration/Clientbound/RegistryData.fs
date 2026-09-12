namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RegistryData =

    let registryData =
        packet "RegistryDataPacket" Configuration Clientbound (Since 764) {
            api [
                field "Codec"    TNbt                           (Between(764, 765))
                field "Registry" TString                        (Since 766)
                field "Entries"  (TArray(TNamed "RegistryEntry")) (Since 766)
            ]
            wire (Between(764, 765)) [ read "codec" AnonNbt "Codec" ]
            wire (Since 766) [
                read "id"      Str                                       "Registry"
                read "entries" (Array(Named "RegistryEntry", VarIntCount)) "Entries"
            ]
        }
