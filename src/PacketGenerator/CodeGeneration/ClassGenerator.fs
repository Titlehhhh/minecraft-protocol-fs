module PacketGenerator.CodeGeneration.ClassGenerator

open Humanizer
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open PacketGenerator.CodeGeneration.Mapping
open Protodef
open PacketGenerator.Protodef


let createProperty (``type``: string) (name: string) =
    SyntaxFactory
        .PropertyDeclaration(SyntaxFactory.ParseTypeName(``type``), name)
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
        .AddAccessorListAccessors(
            SyntaxFactory
                .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            SyntaxFactory
                .AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
        )
    :> MemberDeclarationSyntax

let toProperty (f: FieldDefinition) =
    let clrType = protodefTypeToCSharpType f.OriginalType    
    let name = f.Name
    createProperty clrType name
    

let createAbstractClass (name: string) =
    SyntaxFactory
        .ClassDeclaration(SyntaxFactory.Identifier(name))
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                      SyntaxFactory.Token(SyntaxKind.AbstractKeyword),
                      SyntaxFactory.Token(SyntaxKind.PartialKeyword))
        
let createStruct (name: string) =
    SyntaxFactory
        .StructDeclaration(SyntaxFactory.Identifier(name))
        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                      SyntaxFactory.Token(SyntaxKind.PartialKeyword))

let generate (spec: PacketSpec) =
    let rootClass = createAbstractClass spec.Meta.Name
    
    let toProperties fs = fs |> Seq.map toProperty |> Seq.toArray
    
    let rootClass = rootClass.AddMembers(spec.CommonFields |> toProperties)
    
    let naming interval =
        let str = interval.ToString().Underscore()
        $"V{str}Fields"
    
    let propsForVersioned =
        spec.Versioned |> Seq.map (fun (i, _) ->
            let structName = i |> naming
            let propName = $"V{i.ToString().Underscore()}"
            createProperty structName propName
            )
        |> Seq.toArray
    
    let rootClass = rootClass.AddMembers(propsForVersioned)
    
    let versioned = spec.Versioned |> Seq.map (fun (i, fields) ->
        let name = i |> naming
        let props = fields |> toProperties
        (createStruct name).AddMembers(props) :> MemberDeclarationSyntax) |> Seq.toArray
    
    let rootClass = rootClass.AddMembers(versioned)
    
    
    
    let str = rootClass.NormalizeWhitespace().ToFullString()
    str