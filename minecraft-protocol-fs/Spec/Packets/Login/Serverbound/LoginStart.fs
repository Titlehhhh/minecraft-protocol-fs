namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginStart =

    let loginStart =
        packet "LoginStartPacket" Login Serverbound All {
            api [
                field "Username"   TString                             All
                field "Signature"  (TOption (TNamed "LoginSignature"))  (Between(759, 760))
                field "PlayerUuid" (TOption TUuid)                      (Since 760)
            ]

            wire (Until 758) [
                read "username" Str "Username"
            ]

            wire (Between(759, 759)) [
                read "username"  Str                                 "Username"
                read "signature" (Option (Named "LoginSignature"))   "Signature"
            ]

            wire (Between(760, 760)) [
                read "username"   Str                                "Username"
                read "signature"  (Option (Named "LoginSignature"))   "Signature"
                read "playerUUID" (Option Uuid)                       "PlayerUuid"
            ]

            wire (Between(761, 763)) [
                read "username"   Str           "Username"
                read "playerUUID" (Option Uuid) "PlayerUuid"
            ]

            wire (Since 764) [
                read "username"   Str  "Username"
                read "playerUUID" Uuid "PlayerUuid"
            ]
        }
