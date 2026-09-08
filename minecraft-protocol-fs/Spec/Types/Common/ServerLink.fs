namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerLink =

    let serverLink =
        namedType "ServerLink" {
            api [
                field "Label" (TUnion "ServerLinkLabel") All
                field "Link"  TString All
            ]

            wire (Since 767) [
                read      "hasKnownType" U8  "_hasKnownType"
                readUnion "_hasKnownType" "ServerLinkLabel" "Label"
                read      "link"         Str "Link"
            ]
        }
