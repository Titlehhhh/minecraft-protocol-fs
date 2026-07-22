namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module LoginSuccess =

    let loginSuccess =
        packet "LoginSuccessPacket" Login Clientbound All {
            api [
                field "Uuid"                TUuid                            All
                field "Username"            TString                          All
                field "Properties"          (TArray(TNamed "ProfileProperty")) (Since 759)
                field "StrictErrorHandling" TBool                            (Between(766, 767))
            ]

            wire (Until 758) [
                read "uuid"     Uuid "Uuid"
                read "username" Str  "Username"
            ]

            wire (Between(759, 765)) [
                read "uuid"       Uuid "Uuid"
                read "username"   Str  "Username"
                read "properties" (Array(Named "ProfileProperty", VarIntCount)) "Properties"
            ]

            wire (Between(766, 767)) [
                read "uuid"                Uuid "Uuid"
                read "username"            Str  "Username"
                read "properties"          (Array(Named "ProfileProperty", VarIntCount)) "Properties"
                read "strictErrorHandling" Bool "StrictErrorHandling"
            ]

            wire (Since 768) [
                read "uuid"       Uuid "Uuid"
                read "username"   Str  "Username"
                read "properties" (Array(Named "ProfileProperty", VarIntCount)) "Properties"
            ]
        }
