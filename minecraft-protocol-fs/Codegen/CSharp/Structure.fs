namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module Structure =

    open type SyntaxFactory
    open Text

    // ----- structure: Roslyn building blocks -----

    /// `[ProtocolSupport(from, to)]` from the union of a set of version ranges.
    let internal supportAttr (s: RuntimeSurface) (ranges: VersionRange list) : AttributeListSyntax =
        let lo, hi = VersionRangeX.span (if List.isEmpty ranges then [ All ] else ranges)
        let loS = lo |> Option.map string |> Option.defaultValue s.StartProtocolConst
        let hiS = hi |> Option.map string |> Option.defaultValue s.LatestProtocolConst

        AttributeList(
            SingletonSeparatedList(
                Attribute(ParseName s.SupportAttribute)
                    .WithArgumentList(ParseAttributeArgumentList(sprintf "(%s, %s)" loS hiS))
            )
        )

    /// Parse statement / `// comment` lines into a method body. Roslyn re-indents at the end.
    let internal parseBody (lines: string list) : BlockSyntax =
        let text = "{\n" + String.concat "\n" lines + "\n}"

        match ParseStatement text with
        | :? BlockSyntax as b -> b
        | other -> failwithf "codegen: body did not parse as a block:\n%O" other

    /// `public static T Read(ref Reader reader, int protocolVersion)`
    let internal readMethod (s: RuntimeSurface) (typeName: string) (body: BlockSyntax) : MemberDeclarationSyntax =
        MethodDeclaration(ParseTypeName typeName, s.ReadMethodName)
            .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
            .AddParameterListParameters(
                Parameter(Identifier s.ReaderParam)
                    .WithType(ParseTypeName s.ReaderType)
                    .AddModifiers(Token SyntaxKind.RefKeyword),
                Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int")
            )
            .WithBody(body)

    /// `public [readonly] void Write(Writer writer, int protocolVersion)`
    let internal writeMethod (s: RuntimeSurface) (readonlyValue: bool) (body: BlockSyntax) : MemberDeclarationSyntax =
        let modifiers =
            if readonlyValue then
                [| Token SyntaxKind.PublicKeyword; Token SyntaxKind.ReadOnlyKeyword |]
            else
                [| Token SyntaxKind.PublicKeyword |]

        MethodDeclaration(PredefinedType(Token SyntaxKind.VoidKeyword), s.WriteMethodName)
            .AddModifiers(modifiers)
            .AddParameterListParameters(
                Parameter(Identifier s.WriterParam).WithType(ParseTypeName s.WriterType),
                Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int")
            )
            .WithBody(body)

    let internal baseTypesFor (iface: string option) (name: string) : BaseTypeSyntax[] =
        match iface with
        | Some i -> [| SimpleBaseType(ParseTypeName(sprintf "%s<%s>" i name)) :> BaseTypeSyntax |]
        | None -> [||]

    /// `public readonly partial record struct Name(T A, U B) : IProtocolType<Name> { ... }`
    let internal recordStructShell
        (iface: string option)
        (name: string)
        (positional: (string * string) list)
        : TypeDeclarationSyntax
        =
        let ps =
            positional
            |> List.map (fun (typ, pname) -> Parameter(Identifier pname).WithType(ParseTypeName typ))
            |> List.toArray

        RecordDeclaration(SyntaxKind.RecordStructDeclaration, Token SyntaxKind.RecordKeyword, Identifier name)
            .WithClassOrStructKeyword(Token SyntaxKind.StructKeyword)
            .AddModifiers(
                Token SyntaxKind.PublicKeyword,
                Token SyntaxKind.ReadOnlyKeyword,
                Token SyntaxKind.PartialKeyword
            )
            .AddParameterListParameters(ps)
            .AddBaseListTypes(baseTypesFor iface name)
            .WithOpenBraceToken(Token SyntaxKind.OpenBraceToken)
            .WithCloseBraceToken(Token SyntaxKind.CloseBraceToken)
        :> TypeDeclarationSyntax

    /// `public sealed partial class Name : IProtocolType<Name> { get-only props + constructor }`
    let internal classShell
        (iface: string option)
        (name: string)
        (fields: (string * string) list)
        : TypeDeclarationSyntax
        =
        let props: MemberDeclarationSyntax list =
            [
                for typ, fname in fields ->
                    PropertyDeclaration(ParseTypeName typ, fname)
                        .AddModifiers(Token SyntaxKind.PublicKeyword)
                        .AddAccessorListAccessors(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(Token SyntaxKind.SemicolonToken)
                        )
            ]

        let ctor: MemberDeclarationSyntax =
            ConstructorDeclaration(Identifier name)
                .AddModifiers(Token SyntaxKind.PublicKeyword)
                .AddParameterListParameters(
                    [|
                        for typ, fname in fields -> Parameter(Identifier(camel fname)).WithType(ParseTypeName typ)
                    |]
                )
                .WithBody(parseBody [ for _, fname in fields -> sprintf "%s = %s;" fname (camel fname) ])

        ClassDeclaration(name)
            .AddModifiers(
                Token SyntaxKind.PublicKeyword,
                Token SyntaxKind.SealedKeyword,
                Token SyntaxKind.PartialKeyword
            )
            .AddBaseListTypes(baseTypesFor iface name)
            .AddMembers(List.toArray (props @ [ ctor ]))
        :> TypeDeclarationSyntax

    /// Assemble usings + file-scoped namespace + one type into formatted, *validated* source text.
    let internal renderUnit
        (s: RuntimeSurface)
        (ns: string)
        (label: string)
        (usingNames: string list)
        (decl: MemberDeclarationSyntax)
        : string
        =
        let cu =
            CompilationUnit()
                .AddUsings([| for u in usingNames -> UsingDirective(ParseName u) |])
                .AddMembers(FileScopedNamespaceDeclaration(ParseName ns).AddMembers(decl))
                .NormalizeWhitespace("    ", "\n", false)

        let errors =
            cu.GetDiagnostics()
            |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error)
            |> Seq.toList

        if not errors.IsEmpty then
            failwithf
                "codegen: emitted invalid C# for %s:\n%s\n----\n%s"
                label
                (errors |> List.map string |> String.concat "\n")
                (cu.ToFullString())

        blankLineAfterNamespace (cu.ToFullString()) + "\n"
