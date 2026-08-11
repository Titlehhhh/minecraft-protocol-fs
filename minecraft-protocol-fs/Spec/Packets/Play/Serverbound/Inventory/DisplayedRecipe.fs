namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module DisplayedRecipe =

    let displayedRecipe =
        packet "DisplayedRecipePacket" Play Serverbound (Since 751) {
            api [
                field "RecipeId"    TString (Between(751, 767))
                field "RecipeIdInt" TInt    (Since 768)
            ]

            wire (Between(751, 767)) [
                read "recipeId" Str "RecipeId"
            ]

            wire (Since 768) [
                read "recipeId" VarInt "RecipeIdInt"
            ]
        }
