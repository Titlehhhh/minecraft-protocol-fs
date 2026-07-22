module McProtocol.Program

open System.IO
open McProtocol.Dsl
open McProtocol.Spec
open McProtocol.Codegen

let private outputRoot = "generated-csharp"

[<EntryPoint>]
let main argv =
    match argv with
    | [| "gen" |]
    | [| "gen"; _ |] ->
        let target = CSharp.target
        Directory.CreateDirectory outputRoot |> ignore
        let files = Generator.generateProtocol target protocol

        for f in files do
            let full = Path.Combine(outputRoot, f.RelativePath)
            Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
            File.WriteAllText(full, f.Contents)

        printfn "Generated %d file(s) into %s/:" files.Length outputRoot

        for f in files do
            printfn "  %s" f.RelativePath

        // Echo one type to stdout so `dotnet run -- gen Vec4f` shows the result inline.
        match argv with
        | [| _; name |] ->
            files
            |> List.tryFind (fun f -> Path.GetFileNameWithoutExtension f.RelativePath = name)
            |> Option.iter (fun f -> printfn "\n----- %s -----\n%s" f.RelativePath f.Contents)
        | _ -> ()

        0
    | _ ->
        Printer.printProtocol protocol
        0
