module PacketGenerator.Types

open Protodef

type VersionRange =
    { StartVersion: int
      EndVersion: int }

    member this.Contains(version: int) =
        version >= this.StartVersion && version <= this.EndVersion

    member this.ToArray() =
        [| this.StartVersion .. this.EndVersion |]

    override this.ToString() =
        if this.StartVersion = this.EndVersion then
            $"{this.StartVersion}"
        else
            $"{this.StartVersion}-{this.EndVersion}"

    member private this.IsOne = this.StartVersion = this.EndVersion

    member this.Cond(var: string) =
        if this.IsOne then
            $"{var} == {this.StartVersion}"
        else
            $"{var} >= {this.StartVersion} && {var} <= {this.EndVersion}"
    
    member this.CondSw =
        if this.IsOne then
            this.StartVersion.ToString()
        else
            $">= {this.StartVersion} and <= {this.EndVersion}"


type TypeStructureRecord =
    { Interval: VersionRange
      Structure: ProtodefType option }

type TypeStructureHistory = TypeStructureRecord list


type NamePathPair = { Name: string; Path: string }

type TypeFinderResult =
    { Version: int
      Structure: ProtodefType option }
