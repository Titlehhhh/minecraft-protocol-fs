namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module EncryptionRequest =

    let encryptionRequest =
        packet "EncryptionRequestPacket" Login Clientbound All {
            api [
                field "ServerId"           TString All
                field "PublicKey"          TBytes  All
                field "VerifyToken"        TBytes  All
                field "ShouldAuthenticate" TBool   (Since 766)
            ]

            wire (Until 765) [
                read "serverId"    Str       "ServerId"
                read "publicKey"   ByteArray "PublicKey"
                read "verifyToken" ByteArray "VerifyToken"
            ]

            wire (Since 766) [
                read "serverId"           Str       "ServerId"
                read "publicKey"          ByteArray "PublicKey"
                read "verifyToken"        ByteArray "VerifyToken"
                read "shouldAuthenticate" Bool      "ShouldAuthenticate"
            ]
        }
