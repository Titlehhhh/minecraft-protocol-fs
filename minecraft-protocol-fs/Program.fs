module McProtocol.Program

open System.IO
open McProtocol.Dsl
open McProtocol.Spec
open McProtocol.Codegen

let private outputRoot = "generated-csharp"

/// `protocol-ids.json` next to the built exe (published there via the fsproj `Content` item);
/// falls back to the source-tree copy, resolved from this file's own directory so it doesn't
/// depend on the process's current working directory.
let private manifestPath () =
    let published = Path.Combine(System.AppContext.BaseDirectory, "protocol-ids.json")

    if File.Exists published then
        published
    else
        Path.Combine(__SOURCE_DIRECTORY__, "Spec", "protocol-ids.json")

/// Drops a `--out <value>` pair (if present) from a positional-arg list, leaving the rest
/// in order; used both to find the remaining positional args and, by the caller, to look
/// up the value itself via `List.tryFindIndex`.
let rec private stripOut args =
    match args with
    | "--out" :: _ :: tail -> stripOut tail
    | "--out" :: [] -> []
    | x :: tail -> x :: stripOut tail
    | [] -> []

[<EntryPoint>]
let main argv =
    let argList = List.ofArray argv

    match argList with
    | "gen" :: rest ->
        let outDir =
            match rest |> List.tryFindIndex ((=) "--out") with
            | Some i when i + 1 < rest.Length -> rest[i + 1]
            | _ -> outputRoot

        // Echo one type to stdout so `dotnet run -- gen Vec4f [--out dir]` shows the result
        // inline; only when exactly one bare positional name remains after removing `--out <dir>`.
        let echoName =
            match stripOut rest with
            | [ name ] -> Some name
            | _ -> None

        let target = CSharp.target
        Directory.CreateDirectory outDir |> ignore

        let enrichedPackets, idWarnings = PacketIds.enrich (manifestPath ()) protocol.Packets
        let protocol = { protocol with Packets = enrichedPackets }

        for w in idWarnings do
            printfn "warning: %s" w

        let files = Generator.generateProtocol target protocol

        for f in files do
            let full = Path.Combine(outDir, f.RelativePath)
            Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
            File.WriteAllText(full, f.Contents)

        printfn "Generated %d file(s) into %s/:" files.Length outDir

        for f in files do
            printfn "  %s" f.RelativePath

        echoName
        |> Option.bind (fun name ->
            files |> List.tryFind (fun f -> Path.GetFileNameWithoutExtension f.RelativePath = name))
        |> Option.iter (fun f -> printfn "\n----- %s -----\n%s" f.RelativePath f.Contents)

        0
    | _ ->
        Printer.printProtocol protocol
        0
