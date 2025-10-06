module PacketGenerator.CodeGeneration.Naming

open Humanizer


let property (n: string) = n.Pascalize()

let var (n: string) = n.Camelize()

let className (n: string) = n.Pascalize()
