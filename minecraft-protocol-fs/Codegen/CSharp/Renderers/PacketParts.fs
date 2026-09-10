namespace McProtocol.Codegen.CSharpBackend

open McProtocol.Codegen
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.CSharp.Syntax
open McProtocol.Dsl
open McProtocol.Codegen.CSharpSurface

module PacketParts =

    open type SyntaxFactory
    open Structure

    // ----- packets -----

    /// Packets live under `<root>.Packets.<State>.<Direction>` so same-named packets from
    /// different states/directions (e.g. `KeepAlivePacket` in Configuration vs Play) don't collide.
    let internal packetNamespace (s: RuntimeSurface) (p: PacketSpec) : string =
        sprintf "%s.Packets.%A.%A" s.Namespace p.State p.Direction

    /// Manifest id ranges, ascending, with adjacent ranges carrying the same id merged
    /// (755–755 + 756–756 + 757–758 @ 0x21 -> 755–758 @ 0x21).
    let internal coalesceIds (ids: (int * int * int) list) : (int * int * int) list =
        ids
        |> List.sortBy (fun (lo, _, _) -> lo)
        |> List.fold
            (fun acc (lo, hi, id) ->
                match acc with
                | (plo, phi, pid) :: rest when pid = id && phi + 1 = lo -> (plo, hi, pid) :: rest
                | _ -> (lo, hi, id) :: acc)
            []
        |> List.rev

    /// `public static bool TryGetPacketId(int protocolVersion, out int id)` and `GetPacketId`, its
    /// throwing wrapper. Both forward to `PacketRegistry`, whose catalogs already carry the same
    /// coalesced manifest ranges as data. Emitting the ranges a second time as an if-ladder cost
    /// ~13.8k lines and let the two forms drift apart; the lookup runs on the send path, which is
    /// cold, so scanning one packet's ranges is enough.
    let internal packetIdMethods (s: RuntimeSurface) : MemberDeclarationSyntax list =
        let tryBody =
            [ sprintf "return PacketRegistry.TryGetId(Identity, %s, out id);" s.VersionParam ]

        let tryDecl =
            MethodDeclaration(PredefinedType(Token SyntaxKind.BoolKeyword), "TryGetPacketId")
                .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
                .AddParameterListParameters(
                    Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"),
                    Parameter(Identifier "id").WithType(ParseTypeName "int").AddModifiers(Token SyntaxKind.OutKeyword)
                )
                .WithBody(parseBody tryBody)

        let getBody =
            [ sprintf "return PacketRegistry.GetId(Identity, %s);" s.VersionParam ]

        let getDecl =
            MethodDeclaration(PredefinedType(Token SyntaxKind.IntKeyword), "GetPacketId")
                .AddModifiers(Token SyntaxKind.PublicKeyword, Token SyntaxKind.StaticKeyword)
                .AddParameterListParameters(Parameter(Identifier s.VersionParam).WithType(ParseTypeName "int"))
                .WithBody(parseBody getBody)

        [ tryDecl; getDecl ]
