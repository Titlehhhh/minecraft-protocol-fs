module PacketGenerator.CodeGeneration.Naming

open Humanizer
open PacketGenerator.Types
open PacketGenerator.Utils


let property (n: string) = n.Pascalize()

let var (n: string) = n.Camelize()

let className (n: string) = n.Pascalize()

let versionedProperty (v: VersionRange) (n: string) =
    $"V{v.ToString().Underscore()}.{n |> property}"
