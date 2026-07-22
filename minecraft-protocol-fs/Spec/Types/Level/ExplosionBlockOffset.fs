namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ExplosionBlockOffset =

    let explosionBlockOffset =
        record "ExplosionBlockOffset" (Until 767) [
            col "x" I8
            col "y" I8
            col "z" I8
        ]
