namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module ServerLinkType =

    let serverLinkType =
        enumType "ServerLinkType" {
            values (Since 767) VarInt [
                0, "bug_report"
                1, "community_guidelines"
                2, "support"
                3, "status"
                4, "feedback"
                5, "community"
                6, "website"
                7, "forums"
                8, "news"
                9, "announcements"
            ]
        }
