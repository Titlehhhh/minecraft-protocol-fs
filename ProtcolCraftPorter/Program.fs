open System
open System.Collections.Generic
open System.Net.Http
open System.Text

let hashSet = HashSet<string>()

for proto = 340 to 769 do
    Ext.ClientboundPlayPackets(proto)
    |> Seq.iter (fun x -> hashSet.Add(x) |> ignore)

//print
for x in hashSet do
    printfn "%s" x
