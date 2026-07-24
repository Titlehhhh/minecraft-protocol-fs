namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ClientInformation =

    let clientInformation =
        packet "ClientInformationPacket" Configuration Serverbound (Since 764) {
            protoId "settings"
            api [
                field "Locale"              TString All
                field "ViewDistance"        TInt    All
                field "ChatFlags"           TInt    All
                field "ChatColors"          TBool   All
                field "SkinParts"           TInt    All
                field "MainHand"            TInt    All
                field "EnableTextFiltering" TBool   All
                field "EnableServerListing" TBool   All
                field "ParticleStatus"      TInt    (Since 768)
            ]
            wire (Between(764, 767)) [
                read "locale"              Str    "Locale"
                read "viewDistance"        I8     "ViewDistance"
                read "chatFlags"           VarInt "ChatFlags"
                read "chatColors"          Bool   "ChatColors"
                read "skinParts"           U8     "SkinParts"
                read "mainHand"            VarInt "MainHand"
                read "enableTextFiltering" Bool   "EnableTextFiltering"
                read "enableServerListing" Bool   "EnableServerListing"
            ]
            wire (Since 768) [
                read "locale"              Str    "Locale"
                read "viewDistance"        I8     "ViewDistance"
                read "chatFlags"           VarInt "ChatFlags"
                read "chatColors"          Bool   "ChatColors"
                read "skinParts"           U8     "SkinParts"
                read "mainHand"            VarInt "MainHand"
                read "enableTextFiltering" Bool   "EnableTextFiltering"
                read "enableServerListing" Bool   "EnableServerListing"
                read "particleStatus"      VarInt "ParticleStatus"
            ]
        }
