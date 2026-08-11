namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RecipeBookRemove =

    let recipeBookRemove =
        packet "RecipeBookRemovePacket" Play Clientbound (Since 768) {
            api [ field "RecipeIds" (TArray TInt) All ]
            wire (Since 768) [ read "recipeIds" (Array(VarInt, VarIntCount)) "RecipeIds" ]
        }
