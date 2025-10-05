namespace PacketGenerator.CodeGeneration

open System.Runtime.InteropServices
open PacketGenerator.Protodef
open PacketGenerator.Types
open Protodef

type PacketMeta =
  { Name: string
    CanonicalName: string option
    State: string option
    Direction: string option
    Aliases: string list }


type FieldDefinition =
  {
    Name: string
    OriginalType: ProtodefKind
    ClrType: string option    
  }

type PacketSpec =
  { Meta: PacketMeta
    CommonFields: FieldDefinition list
    Versioned: (VersionRange * FieldDefinition list) list }