module PacketGenerator.CodeGeneration.Naming

open Humanizer
open PacketGenerator.Types
open PacketGenerator.Utils


let property (n: string) = n.Pascalize()

let var (n: string) = n.Camelize()

let className (n: string) = n.Pascalize()

let versionedProperty (v: VersionRange) (n: string) =
    $"v{v.ToString().Underscore()}.{n |> property}"

let versionedVar (v: VersionRange) =
    $"v{v.ToString().Underscore()}"

let versionedStruct (v: VersionRange) =
    $"V{v.ToString().Underscore()}"
