module PacketGenerator.Types

open Protodef

type VersionRange =
    { StartVersion: int; EndVersion: int }
    
    member this.Contains(version: int) =
        version >= this.StartVersion && version <= this.EndVersion
    
    member this.ToArray() = [|this.StartVersion..this.EndVersion|]
    override this.ToString() =
        if this.StartVersion = this.EndVersion then
            $"{this.StartVersion}"
        else
            $"{this.StartVersion}-{this.EndVersion}"
            

type TypeStructureRecord =
    { Interval: VersionRange
      Structure: ProtodefType option }

type TypeStructureHistory =
    TypeStructureRecord list
    

type NamePathPair = { Name: string; Path: string }

type TypeFinderResult =
    { Version: int
      Structure: ProtodefType option }