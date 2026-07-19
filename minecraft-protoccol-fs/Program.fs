module McProtocol.Program

open McProtocol.Dsl
open McProtocol.Spec

[<EntryPoint>]
let main _ =
    Printer.printProtocol protocol
    0
