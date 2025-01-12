module MinecraftDataFSharp.PacketFactoryGenerator

open System.Collections.Generic
open Humanizer
open MinecraftDataFSharp.Models

let private createMap (protocol: ProtocolVersionEntry, side: string) =
    let obj =
        (protocol.JsonProtocol["play"][side]["types"]["packet"][1][0]["type"][1]["mappings"])
            .AsObject()

    obj |> Seq.map (fun kv -> (kv.Value.ToString(), kv.Key |> int)) |> Map

let private getClass (p: Packet, version: int) : string option =
    match (p.Structure.Keys |> Seq.tryFind _.Contains(version)) with
    | Some t ->
        let name = p.PacketName.Substring("packet_".Length)
        let name = name.Pascalize()
        let className = $"{name}.V{t}".Replace("-", "_")
        Some(className)
    | None -> None

let create (packets: Packet seq, protocols: ProtocolVersionEntry seq) =
    let arr = ResizeArray()
    let classes = HashSet<string>()

    for proto in protocols do
        let map = createMap (proto, "toClient")

        for p in packets do
            let name = p.PacketName.Substring("packet_".Length)

            match map.TryFind(name) with
            | Some id ->
                let cl = getClass (p, proto.ProtocolVersion)

                match cl with
                | Some cl ->
                    classes.Add(cl) |> ignore
                    let cl = cl.Replace(".", "_") + "Factory"
                    let s = $"{{Combine({proto.ProtocolVersion},{id}), {cl}}}"
                    arr.Add(s)
                | None -> ()
            | None -> ()

            ()

    let factories =
        classes
        |> Seq.map (fun x ->
            let name = x.Replace(".", "_") + "Factory"
            $"public static readonly Func<IServerPacket> {name} = () => new {x}();")
        |> Seq.toArray

    {| Dict = arr.ToArray(); Factories = factories |}
