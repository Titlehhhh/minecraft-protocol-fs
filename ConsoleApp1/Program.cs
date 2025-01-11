using SandBoxLib;
using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;


namespace ConsoleApp1;

public class Program
{
    public static void Main(string[] args)
    {
        var id = SyntaxFactory.Identifier("asd");
        var class1 = SyntaxFactory.ClassDeclaration(id);
        class1.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        var class2 = class1;

        Console.WriteLine(ReferenceEquals(class1, class2));
    }
}