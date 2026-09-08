namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CraftingBookDataAction =

    let craftingBookDataAction =
        unionType "CraftingBookDataAction" {
            cases
                (Until 736)
                [
                    case1 0 "DisplayedRecipe" [ read "displayedRecipe" Str "RecipeId" ]

                    case1
                        1
                        "BookSettings"
                        [
                            read "craftingBookOpen" Bool "CraftingBookOpen"
                            read "craftingFilter" Bool "CraftingFilter"
                            read "smeltingBookOpen" Bool "SmeltingBookOpen"
                            read "smeltingFilter" Bool "SmeltingFilter"
                            read "blastingBookOpen" Bool "BlastingBookOpen"
                            read "blastingFilter" Bool "BlastingFilter"
                            read "smokingBookOpen" Bool "SmokingBookOpen"
                            read "smokingFilter" Bool "SmokingFilter"
                        ]
                ]
        }
