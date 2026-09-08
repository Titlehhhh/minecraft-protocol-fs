namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module FacePlayerEntityTarget =

    let facePlayerEntityTarget =
        namedType "FacePlayerEntityTarget" {
            api [
                field "EntityId"     TInt All
                field "FeetEyesName" TString (Until 764)
                field "FeetEyes"     TInt (Since 765)
            ]

            wire (Until 764) [
                read "entityId"         VarInt "EntityId"
                read "entity_feet_eyes" Str    "FeetEyesName"
            ]

            wire (Since 765) [
                read "entityId"         VarInt "EntityId"
                read "entity_feet_eyes" VarInt "FeetEyes"
            ]
        }
