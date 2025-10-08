namespace PacketGenerator.CodeGeneration

open System.Runtime.InteropServices
open PacketGenerator.Protodef
open PacketGenerator.Types
open PacketGenerator.Utils
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
    OriginalType: ProtodefType
    Kind: ProtodefKind
    ClrType: string option
    IsCommon: bool
  }

type ClassSpec =
  { Meta: PacketMeta
    CommonFields: FieldDefinition list
    Versioned: (VersionRange * FieldDefinition list) list
    Ordered: (VersionRange * FieldDefinition list) list }