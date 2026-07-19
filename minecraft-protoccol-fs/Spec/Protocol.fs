namespace McProtocol.Spec

open McProtocol.Dsl

/// The assembled protocol: every named type, union and packet described so far.
/// Add a new spec file, then register its binding here.
[<AutoOpen>]
module Protocol =

    let protocol =
        {
            Types = [
                mapColorData
                rotations
                villagerData
                entityMetadataEntry
            ]

            Unions = [
                teamAction
                entityMetadataValue
            ]

            Packets = [
                unloadChunk
                mapPacket
                teamsPacket
                entityMetadataPacket
                windowClick
            ]
        }
