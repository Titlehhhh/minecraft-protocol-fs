namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RecipeBookSetting =

    let recipeBookSetting =
        record "RecipeBookSetting" (Since 771) [
            col "open"      Bool
            col "filtering" Bool
        ]
