
[<AutoOpen>]
module AbsolutePathExtensions

open System.Linq
open TruePath
open TruePath.SystemIo


type AbsolutePath with
    member this.CreateClearDirectory(): unit =
        if this.ExistsDirectory() then
            this.DeleteDirectoryRecursively()
        this.CreateDirectory()

let uniquePaths (paths: AbsolutePath array) =
    paths.ToHashSet(AbsolutePath.StrictStringComparer) |> Seq.toArray
    
let createClearPaths (paths: AbsolutePath array) : unit =
    paths |> Seq.iter _.CreateClearDirectory()