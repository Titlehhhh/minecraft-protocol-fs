namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RespawnData =

    let respawnData =
        record "RespawnData" (Since 773) [ col "globalPos" (Named "GlobalPos"); col "yaw" F32; col "pitch" F32 ]
