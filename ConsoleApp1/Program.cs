using Acornima.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


var method = SyntaxFactory.MethodDeclaration(
        SyntaxFactory.ParseTypeName("void"), "MyMethod")
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    .AddParameterListParameters(
        SyntaxFactory.Parameter(SyntaxFactory.Identifier("arg1"))
            .WithType(SyntaxFactory.ParseTypeName("int[]")),
        SyntaxFactory.Parameter(SyntaxFactory.Identifier("arg2"))
            .WithType(SyntaxFactory.ParseTypeName("string")))
    .WithBody(
        SyntaxFactory.Block(
            SyntaxFactory.ParseStatement("Console.WriteLine(arg2);")));

Console.WriteLine(method.NormalizeWhitespace().ToFullString());


var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("MyNamespace"))
    .AddMembers(
        SyntaxFactory.ClassDeclaration("MyClass")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddMembers(
                SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.ParseTypeName("void"), "PrintMessage")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                            .WithType(SyntaxFactory.ParseTypeName("string")))
                    .WithBody(
                        SyntaxFactory.Block(
                            SyntaxFactory.ParseStatement("Console.WriteLine(message);"))),
                SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.ParseTypeName("int"), "Add")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("a"))
                            .WithType(SyntaxFactory.ParseTypeName("int")),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("b"))
                            .WithType(SyntaxFactory.ParseTypeName("int")))
                    .WithBody(
                        SyntaxFactory.Block(
                            SyntaxFactory.ParseStatement("return a + b;")))
            )
    );

Console.WriteLine(namespaceDeclaration.NormalizeWhitespace().ToFullString());