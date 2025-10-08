module PacketGenerator.CodeGeneration.WriteGen

open System
open Humanizer
open PacketGenerator.CodeGeneration.Mapping
open PacketGenerator.Protodef
open PacketGenerator.Types
open Microsoft.CodeAnalysis.CSharp.Syntax
open PacketGenerator.Utils
open System.Collections.Generic
open Protodef


let supportedCustomTypes =
    [|
        "vec2f";
         "vec3f" ;
         "vec3f64";
         "vec4f";
         "position";
         "Slot" ;
         "MovementFlags" ;
        "PositionUpdateRelatives";
        "ContainerID" ;
    |]

let (|SupportedCustom|_|) (t: ProtodefCustomType) =
    if supportedCustomTypes |> Array.exists _.Equals(t.Name, StringComparison.OrdinalIgnoreCase) then
        Some t.Name
    else None

let (|SpecialCustom|_|) (expected: string) (t: ProtodefCustomType) =
    if t.Name.Equals(expected, StringComparison.OrdinalIgnoreCase) then Some()
    else None

let (|UnsupportedCustom|) (t: ProtodefCustomType) =
    t.Name

let writeInsForField (field: FieldDefinition) =
    
    let name = field.Name |> Naming.var
    
    let parse f = SF.ParseStatement f
    
    let list = List<StatementSyntax>()
    
    let add (m: string) =
        parse m |> list.Add
    

    let wi (val': string) (met: string) =
        parse $"writer.{met}({val'});" |> list.Add

    let wiP (val': string) (met: string) =
        parse $"writer.{met}({val'}, protocolVersion);" |> list.Add
        
    
    
    let wiFor (protoDefName) =
        TypeToWriteMethodMap[protoDefName] |> wi name 
    
    match field.Kind with
    | NumericType num -> wiFor num.ProtodefName
    | VarInt _-> wiFor "varint"
    | VarLong _ -> wiFor "varlong"
    | String _ -> wiFor "string"
    | PrefixedString _ -> wiFor "pstring"
    | Bool _ -> wiFor "bool"
    | CustomType cus ->
        match cus with
        | SupportedCustom c ->
            let pascal = c.Pascalize()
            add $"writer.WriteType<{pascal}>({name}, protocolVersion);"
        | SpecialCustom "UUID" _ ->
            wiFor "UUID"
        | SpecialCustom "restBuffer" ->
            wiFor "restBuffer"
        | SpecialCustom "ByteArray" ->
            wi name "WriteBuffer<VarInt>"
            
    | Option op ->
        add $"writer.WriteOptional({name}, protocolVersion)"
    | Buffer buff ->
        match buff with
        | BufferCount Rest -> wiFor "restBuffer"
        | BufferCount (Fixed n) -> add $"writer.WriteBuffer({name}, {n});"
        | BufferCount (Field f) -> add $"writer.WriteBuffer({name}, {f});"
        | BufferCount (Number n) -> add $"writer.WriteBuffer<VarInt>({name});"
    
    
    list
    


let getName (field: FieldDefinition) (naming: string -> string) : string =
    if field.IsCommon then
        field.Name |> Naming.property
    else field.Name |> naming

let genWriteInstcs (fields: FieldDefinition list) (naming: string -> string) =
    for field in fields do
        let identifier = getName field naming
        
    
    

let generateWrite (fields: (VersionRange * FieldDefinition list) list) =
    ()