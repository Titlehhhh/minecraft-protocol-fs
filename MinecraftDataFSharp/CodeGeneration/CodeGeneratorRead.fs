module MinecraftDataFSharp.CodeGeneration.CodeGeneratorRead

open MinecraftDataFSharp.Models

let generatePrimitive (packets: PacketMetadata list, folder: string) =
    
    ignore