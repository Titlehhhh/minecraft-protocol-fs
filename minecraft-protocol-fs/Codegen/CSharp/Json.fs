namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

/// The JSON *model view* of a generated type: `WriteJson(Utf8JsonWriter)` writes the already
/// decoded value, never the wire. It is version-free by construction — the value has picked its
/// layer, its union case and its optionals when it was read, so the writer only follows what is
/// there. Version layers flatten into the parent object, a union case is named by its DSL name
/// (no layer suffix), a null optional is left out.
module Json =

    open type SyntaxFactory
    open Text
    open Structure

    /// What a value looks like in JSON, independent of whether it came from an api or a wire type.
    type JsonShape =
        | JBool
        | JNumber
        /// A float or double: JSON has no NaN or infinity, so those go out as strings.
        | JFloat
        | JString
        | JUuid
        | JBytes
        | JEnum
        /// Anything with its own `WriteJson`: named types, bitflags, holders, unions, NBT.
        | JObject
        | JArray of JsonShape
        | JOption of JsonShape

    let rec internal shapeOfApi (t: ApiType) : JsonShape =
        match t with
        | TBool -> JBool
        | TInt
        | TLong -> JNumber
        | TFloat
        | TDouble -> JFloat
        | TString -> JString
        | TUuid -> JUuid
        | TNbt -> JObject
        | TBytes -> JBytes
        | TArray inner -> JArray(shapeOfApi inner)
        | TOption inner -> JOption(shapeOfApi inner)
        | THolder _ -> JObject
        | TNamed _ -> JObject
        | TUnion _ -> JObject
        | TEnum _ -> JEnum

    /// Shape of a wire field held as-is (what a union case declares). `None` mirrors
    /// `Statements.wireCsType`: a shape with no C# spelling has no JSON either.
    let rec internal shapeOfWire (s: RuntimeSurface) (w: WireType) : JsonShape option =
        match w with
        | Void
        | Switch _ -> None
        | Bool -> Some JBool
        | VarInt
        | VarLong
        | I8
        | U8
        | I16
        | U16
        | I32
        | U32
        | I64
        | U64 -> Some JNumber
        | F32
        | F64 -> Some JFloat
        | Str -> Some JString
        | Uuid -> Some JUuid
        | Nbt
        | AnonNbt -> Some JObject
        | ByteArray
        | FixedBytes _
        | RestBytes
        | Array(U8, VarIntCount) -> Some JBytes
        | Array(item, _)
        | SentinelArray(item, _) -> shapeOfWire s item |> Option.map JArray
        | Option inner -> shapeOfWire s inner |> Option.map JOption
        | RegistryHolder inner -> holderCsType s inner |> Option.map (fun _ -> JObject)
        | Named _ -> Some JObject
        | EnumRef _ -> Some JEnum

    /// Statements that write one value at the writer's current position. `depth` keeps the
    /// loop and pattern locals of nested arrays/optionals apart.
    let rec internal valueLines (s: RuntimeSurface) (depth: int) (shape: JsonShape) (expr: string) : string list =
        let w = s.JsonWriterParam

        match shape with
        | JBool -> [ sprintf "%s.WriteBooleanValue(%s);" w expr ]
        | JNumber -> [ sprintf "%s.WriteNumberValue(%s);" w expr ]
        | JFloat ->
            [
                sprintf "if (double.IsFinite(%s)) %s.WriteNumberValue(%s);" expr w expr
                sprintf
                    "else %s.WriteStringValue(double.IsNaN(%s) ? \"NaN\" : %s > 0 ? \"Infinity\" : \"-Infinity\");"
                    w
                    expr
                    expr
            ]
        | JString
        | JUuid -> [ sprintf "%s.WriteStringValue(%s);" w expr ]
        | JBytes -> [ sprintf "%s.WriteBase64StringValue(%s);" w expr ]
        | JEnum -> [ sprintf "%s.WriteStringValue(%s.ToString());" w expr ]
        | JObject -> [ sprintf "%s.%s(%s);" expr s.WriteJsonMethodName w ]
        | JArray item ->
            let it = sprintf "item%d" depth

            [ sprintf "%s.WriteStartArray();" w; sprintf "foreach (var %s in %s)" it expr; "{" ]
            @ valueLines s (depth + 1) item it
            @ [ "}"; sprintf "%s.WriteEndArray();" w ]
        | JOption inner ->
            let v = sprintf "value%d" depth

            [ sprintf "if (%s is { } %s)" expr v; "{" ]
            @ valueLines s (depth + 1) inner v
            @ [ "}"; "else"; "{"; sprintf "%s.WriteNullValue();" w; "}" ]

    /// One property of an object. An optional that is null is not written at all: in the model
    /// view "absent on this version" and "not sent" are the same thing — nothing.
    let internal propertyLines (s: RuntimeSurface) (name: string) (shape: JsonShape) (expr: string) : string list =
        let w = s.JsonWriterParam

        match shape with
        | JOption inner ->
            let v = sprintf "%sValue" (camel name)

            [
                sprintf "if (%s is { } %s)" expr v
                "{"
                sprintf "%s.WritePropertyName(\"%s\");" w name
            ]
            @ valueLines s 0 inner v
            @ [ "}" ]
        | _ -> sprintf "%s.WritePropertyName(\"%s\");" w name :: valueLines s 0 shape expr

    /// `(name, shape, expression)` triples as one JSON object.
    let internal objectLines (s: RuntimeSurface) (props: (string * JsonShape * string) list) : string list =
        let w = s.JsonWriterParam

        [ sprintf "%s.WriteStartObject();" w ]
        @ (props |> List.collect (fun (n, sh, e) -> propertyLines s n sh e))
        @ [ sprintf "%s.WriteEndObject();" w ]

    /// `public [readonly] void WriteJson(Utf8JsonWriter writer)`
    let internal writeJsonMethod (s: RuntimeSurface) (readonlyValue: bool) (body: BlockSyntax) : MemberDeclarationSyntax =
        let modifiers =
            if readonlyValue then
                [| Token SyntaxKind.PublicKeyword; Token SyntaxKind.ReadOnlyKeyword |]
            else
                [| Token SyntaxKind.PublicKeyword |]

        MethodDeclaration(PredefinedType(Token SyntaxKind.VoidKeyword), s.WriteJsonMethodName)
            .AddModifiers(modifiers)
            .AddParameterListParameters(
                Parameter(Identifier s.JsonWriterParam).WithType(ParseTypeName s.JsonWriterType)
            )
            .WithBody(body)
