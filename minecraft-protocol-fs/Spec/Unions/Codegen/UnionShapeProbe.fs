namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module UnionShapeProbe =

    let unionShapeProbe =
        unionType "UnionShapeProbe" {
            cases All [
                case1 0 "Byte" [ read "value" I8 "Value" ]
                case1 1 "Int" [ read "value" VarInt "Value" ]
                case1 2 "String" [ read "value" Str "Value" ]
                case1 3 "Rotations" [ read "value" (Named "Rotations") "Value" ]
                case1 4 "Vec3f" [ read "value" (Array(Named "Vec3f", VarIntCount)) "Value" ]
            ]
        }
