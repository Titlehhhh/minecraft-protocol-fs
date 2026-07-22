namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DamageEvent =

    let damageEvent =
        packet "DamageEventPacket" Play Clientbound (Since 762) {
            api [
                field "EntityId"       TInt All
                field "SourceTypeId"   TInt All
                field "SourceCauseId"  TInt All
                field "SourceDirectId" TInt All
                field "SourcePosition" (TOption(TNamed "Vec3f64")) All
            ]

            wire (Since 762) [
                read "entityId"       VarInt "EntityId"
                read "sourceTypeId"   VarInt "SourceTypeId"
                read "sourceCauseId"  VarInt "SourceCauseId"
                read "sourceDirectId" VarInt "SourceDirectId"
                read "sourcePosition" (Option(Named "Vec3f64")) "SourcePosition"
            ]
        }
