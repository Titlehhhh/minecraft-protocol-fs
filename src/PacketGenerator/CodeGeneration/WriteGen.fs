module PacketGenerator.CodeGeneration.WriteGen

open System
open System.Text
open Humanizer
open Microsoft.CodeAnalysis.CSharp
open PacketGenerator.CodeGeneration.Mapping
open PacketGenerator.Protodef
open PacketGenerator.Types
open Microsoft.CodeAnalysis.CSharp.Syntax
open PacketGenerator.Utils
open System.Collections.Generic
open Protodef


let supportedCustomTypes =
    [| "vec2f"
       "vec3f"
       "vec3f64"
       "vec4f"
       "position"
       "Slot"
       "MovementFlags"
       "PositionUpdateRelatives"
       "ContainerID" |]

let (|SupportedCustom|_|) (t: ProtodefCustomType) =
    if
        supportedCustomTypes
        |> Array.exists _.Equals(t.Name, StringComparison.OrdinalIgnoreCase)
    then
        Some t.Name
    else
        None

let (|SpecialCustom|_|) (expected: string) (t: ProtodefCustomType) =
    if t.Name.Equals(expected, StringComparison.OrdinalIgnoreCase) then
        Some()
    else
        None

let (|UnsupportedCustom|) (t: ProtodefCustomType) = t.Name

let rec genWriteExpr (exprName: string) (typ: ProtodefType) : StatementSyntax list =
    let parse f = SF.ParseStatement f

    let kind =
        match typ with
        | Protodef kind -> kind


    match kind with
    | NumericType num -> [ parse $"writer.{TypeToWriteMethodMap[num.ProtodefName]}({exprName});" ]

    | VarInt _ -> [ parse $"writer.WriteVarInt({exprName});" ]

    | VarLong _ -> [ parse $"writer.WriteVarLong({exprName});" ]

    | String _ -> [ parse $"writer.WriteString({exprName});" ]

    | Bool _ -> [ parse $"writer.WriteBool({exprName});" ]

    | CustomType c ->
        match c with
        | SupportedCustom name -> [ parse $"writer.WriteType<{name.Pascalize()}>({exprName}, protocolVersion);" ]
        | SpecialCustom "UUID" _ -> [ parse $"writer.WriteUUID({exprName});" ]
        | SpecialCustom "ByteArray" -> [ parse $"writer.WriteBuffer<VarInt>({exprName});" ]
        | _ -> [ parse $"writer.WriteType<{c.Name}>({exprName}, protocolVersion);" ]

    | Option opt ->
        // рекурсивно пишем Optional, вложенный тип — тот же
        [ parse $"writer.WriteOptional({exprName}, protocolVersion, static writer =>" ]
        @ (genWriteExpr "writer" opt.Type)
        @ [ parse ");" ]

    // | Array arr ->
    //     match arr.Count with
    //     | BufferCount.Rest ->
    //         [ parse $"writer.WriteRestBuffer({exprName});" ]
    //     | BufferCount.Fixed n ->
    //         [ parse $"writer.WriteArrayFixed({exprName}, {n}, static (writer, elem) =>" ]
    //         @ (genWriteExpr "elem" arr.Type)
    //         @ [ parse ");" ]
    //     | BufferCount.Field lenField ->
    //         [ parse $"writer.WriteArrayWithLength({exprName}, {lenField}, static (writer, elem) =>" ]
    //         @ (genWriteExpr "elem" arr.Type)
    //         @ [ parse ");" ]
    //     | BufferCount.Number _ ->
    //         // Для varint и varlong массивов делаем особые вызовы
    //         match arr.Type with
    //         | VarInt _ ->
    //             [ parse $"writer.WriteVarIntArray({exprName});" ]
    //         | VarLong _ ->
    //             [ parse $"writer.WriteVarLongArray({exprName});" ]
    //         | _ ->
    //             [ parse $"writer.WriteArray({exprName}, static (writer, elem) =>" ]
    //             @ (genWriteExpr "elem" arr.Type)
    //             @ [ parse ");" ]


    | Buffer buff ->
        match buff with
        | BufferCount BufferCount.Rest -> [ parse $"writer.WriteRestBuffer({exprName});" ]
        | BufferCount(BufferCount.Fixed n) -> [ parse $"writer.WriteBuffer({exprName}, {n});" ]
        | BufferCount(BufferCount.Field f) -> [ parse $"writer.WriteBuffer({exprName}, {f});" ]
        | BufferCount(BufferCount.Number _) -> [ parse $"writer.WriteBuffer<VarInt>({exprName});" ]
    | _ -> failwith "GG"

let writeInsForField (name: string) (typ: ProtodefType) =
    let name = name |> Naming.var
    genWriteExpr name typ

let getName (field: FieldDefinition) (naming: string -> string) : string =
    if field.IsCommon then
        field.Name |> Naming.property
    else
        field.Name |> naming

let genWriteInstcs (fields: FieldDefinition list) (naming: string -> string) =
    fields
    |> Seq.map (fun field ->
        let identifier = getName field naming
        writeInsForField identifier field.OriginalType)
    |> Seq.toList
    |> List.collect id



let generateWrite (fields: (VersionRange * FieldDefinition list) list) : MemberDeclarationSyntax =
    // создаём метод-заготовку
    let methodText = "public void Write(ref AbstractWriter writer, int protocolVersion)"

    let methodDecl = SF.ParseMemberDeclaration(methodText) :?> MethodDeclarationSyntax

    let sb = StringBuilder()
    sb.AppendLine("switch (protocolVersion)") |> ignore
    sb.AppendLine("{") |> ignore


    for vrange, fieldList in fields do
        let naming = Naming.versionedProperty vrange

        let fieldStmts =
            fieldList
            |> List.collect (fun f ->
                let name = getName f naming
                genWriteExpr name f.OriginalType)

        let cond = vrange.CondSw
        sb.AppendLine($"case {cond}:") |> ignore
        sb.AppendLine("{") |> ignore
        let varStruct = vrange |> Naming.versionedVar
        let propStruct = vrange |> Naming.versionedStruct
        sb.AppendLine($"var {varStruct} = {propStruct}.GetValueOrDefault();")
        |> ignore

        fieldStmts |> Seq.iter (fun f -> sb.AppendLine(f.ToFullString()) |> ignore)
        sb.AppendLine("break;") |> ignore
        sb.AppendLine("}") |> ignore


    sb.AppendLine("}") |> ignore

    let bodyStmt = (SF.ParseStatement(sb.ToString())) :> StatementSyntax

    let bodyStmt = SF.Block(bodyStmt)

    methodDecl.WithBody(bodyStmt)
