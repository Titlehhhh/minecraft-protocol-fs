namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Text =

    open type SyntaxFactory

    // ----- small text helpers -----

    /// C# reserved keywords; a camel-cased identifier that collides with one must be verbatim
    /// (`@namespace`), else Roslyn emits the bare keyword and the file fails to parse.
    let private csharpKeywords =
        set
            [
                "abstract"
                "as"
                "base"
                "bool"
                "break"
                "byte"
                "case"
                "catch"
                "char"
                "checked"
                "class"
                "const"
                "continue"
                "decimal"
                "default"
                "delegate"
                "do"
                "double"
                "else"
                "enum"
                "event"
                "explicit"
                "extern"
                "false"
                "finally"
                "fixed"
                "float"
                "for"
                "foreach"
                "goto"
                "if"
                "implicit"
                "in"
                "int"
                "interface"
                "internal"
                "is"
                "lock"
                "long"
                "namespace"
                "new"
                "null"
                "object"
                "operator"
                "out"
                "override"
                "params"
                "private"
                "protected"
                "public"
                "readonly"
                "ref"
                "return"
                "sbyte"
                "sealed"
                "short"
                "sizeof"
                "stackalloc"
                "static"
                "string"
                "struct"
                "switch"
                "this"
                "throw"
                "true"
                "try"
                "typeof"
                "uint"
                "ulong"
                "unchecked"
                "unsafe"
                "ushort"
                "using"
                "virtual"
                "void"
                "volatile"
                "while"
            ]

    let internal camel (s: string) =
        let n =
            if s.Length = 0 || s.[0] = '_' then
                s
            else
                string (System.Char.ToLower s.[0]) + s.[1..]

        if csharpKeywords.Contains n then "@" + n else n

    let internal pascal (s: string) =
        if s.Length = 0 then
            s
        else
            string (System.Char.ToUpper s.[0]) + s.[1..]

    /// Comments must stay one line — `%A` of a nested entry may print across several.
    let private oneLine (s: string) = s.Replace("\r", " ").Replace("\n", " ")

    let internal todoLine (what: string) =
        sprintf "// TODO(codegen): %s" (oneLine what)

    /// A stub branch must fail loudly at runtime: silently reading/writing a partial wire
    /// (the pre-2026-08-01 behaviour) corrupts the stream for that protocol version.
    let internal throwTodoLine (typeName: string) =
        sprintf
            "throw new System.NotImplementedException(\"TODO(codegen): %s wire layout is not fully generated for this protocol version.\");"
            typeName

    /// Value types become `record struct`; everything else a `sealed class` (mirrors McProtoNet).
    let internal isValue =
        function
        | TBool
        | TInt
        | TLong
        | TFloat
        | TDouble
        | TUuid
        | TEnum _ -> true
        | _ -> false

    /// A version range as a C# boolean condition, or None when it always applies.
    let internal guardCondition (s: RuntimeSurface) (range: VersionRange) : string option =
        match VersionRangeX.bounds range with
        | None, None -> None
        | Some lo, None -> Some(sprintf "%s >= %d" s.VersionParam lo)
        | None, Some hi -> Some(sprintf "%s <= %d" s.VersionParam hi)
        | Some lo, Some hi -> Some(sprintf "%s >= %d && %s <= %d" s.VersionParam lo s.VersionParam hi)

    let internal gateLine (s: RuntimeSurface) (typeName: string) =
        sprintf "%s<%s>(%s);" s.ThrowIfNotSupported typeName s.VersionParam

    let internal throwNoLayoutLine (s: RuntimeSurface) (typeName: string) =
        sprintf
            "throw new System.NotSupportedException($\"%s has no wire layout for protocol version {%s}.\");"
            typeName
            s.VersionParam

    /// A discriminator the layer has no arm for: a stream condition, not a codegen gap.
    let internal throwNoCaseLine (s: RuntimeSurface) (typeName: string) =
        sprintf
            "throw new System.NotSupportedException($\"%s has no case for discriminator {%s} at protocol version {%s}.\");"
            typeName
            s.DiscriminatorParam
            s.VersionParam

    /// A discriminator no arm of an inline union claims. The same stream condition as the
    /// named-union case, but an inline union has no type to name — its discriminator's own DSL
    /// name is the only anchor the message can carry.
    let internal throwNoInlineCaseLine (s: RuntimeSurface) (disc: string) (value: string) =
        sprintf
            "throw new System.NotSupportedException($\"Inline union on '%s' has no case for {%s} at protocol version {%s}.\");"
            disc
            value
            s.VersionParam

    /// A case whose layer does not cover the version being written: the model holds a shape this
    /// version cannot carry, so the write must fail rather than invent one.
    let internal throwNoCaseLayerLine (s: RuntimeSurface) (typeName: string) =
        sprintf
            "throw new System.NotSupportedException($\"%s case {GetType().Name} has no wire layout for protocol version {%s}.\");"
            typeName
            s.VersionParam

    /// Local variable name for an api field; dodges the generated method's own parameter names.
    let internal localName (s: RuntimeSurface) (api: string) =
        let n = camel api

        if n = s.VersionParam || n = s.ReaderParam || n = s.WriterParam then
            n + "_"
        else
            n

    /// Roslyn's NormalizeWhitespace ends a file-scoped namespace with no blank line, gluing the
    /// declaration to the attributes or the type that follow it.
    let internal blankLineAfterNamespace (text: string) : string =
        let rec insert acc rest =
            match rest with
            | (l: string) :: tail when l.StartsWith "namespace " && l.EndsWith ";" ->
                List.rev acc @ (l :: "" :: tail)
            | l :: tail -> insert (l :: acc) tail
            | [] -> List.rev acc

        text.Split('\n') |> Array.toList |> insert [] |> String.concat "\n"
