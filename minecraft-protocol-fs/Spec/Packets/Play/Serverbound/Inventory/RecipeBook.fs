namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RecipeBook =

    let recipeBook =
        packet "RecipeBookPacket" Play Serverbound (Since 751) {
            api [
                field "BookId"       TInt  All
                field "BookOpen"     TBool All
                field "FilterActive" TBool All
            ]

            wire (Since 751) [
                read "bookId"       VarInt "BookId"
                read "bookOpen"     Bool   "BookOpen"
                read "filterActive" Bool   "FilterActive"
            ]
        }
