namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module CraftRecipeRequest =

    let craftRecipeRequest =
        packet "CraftRecipeRequestPacket" Play Serverbound All {
            api [
                field "WindowId" TInt    All
                field "Recipe"   TString (Until 767)
                field "RecipeId" TInt    (Since 768)
                field "MakeAll"  TBool   All
            ]

            wire (Until 766) [
                read "windowId" I8   "WindowId"
                read "recipe"   Str  "Recipe"
                read "makeAll"  Bool "MakeAll"
            ]

            wire (Between(767, 767)) [
                read "windowId" U8   "WindowId"
                read "recipe"   Str  "Recipe"
                read "makeAll"  Bool "MakeAll"
            ]

            wire (Since 768) [
                read "windowId" VarInt "WindowId"
                read "recipeId" VarInt "RecipeId"
                read "makeAll"  Bool   "MakeAll"
            ]
        }
