namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerLinkLabel =

    let serverLinkLabel =
        unionType "ServerLinkLabel" {
            cases
                (Since 767)
                [
                    case1 1 "KnownType" [ read "knownType" (enumOf "ServerLinkType") "Type" ]
                    case1 0 "Custom" [ read "label" AnonNbt "Label" ]
                ]
        }
