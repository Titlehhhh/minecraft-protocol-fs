namespace McProtocol.Spec

open McProtocol.Dsl

[<AutoOpen>]
module RecipeBookSettings =

    let recipeBookSettings =
        packet "RecipeBookSettingsPacket" Play Clientbound (Since 768) {
            api [
                field "CraftingGuiOpen"            TBool (Between(768, 770))
                field "CraftingFilteringCraftable" TBool (Between(768, 770))
                field "SmeltingGuiOpen"            TBool (Between(768, 770))
                field "SmeltingFilteringCraftable" TBool (Between(768, 770))
                field "BlastGuiOpen"               TBool (Between(768, 770))
                field "BlastFilteringCraftable"    TBool (Between(768, 770))
                field "SmokerGuiOpen"              TBool (Between(768, 770))
                field "SmokerFilteringCraftable"   TBool (Between(768, 770))
                field "Crafting" (TNamed "RecipeBookSetting") (Since 771)
                field "Furnace"  (TNamed "RecipeBookSetting") (Since 771)
                field "Blast"    (TNamed "RecipeBookSetting") (Since 771)
                field "Smoker"   (TNamed "RecipeBookSetting") (Since 771)
            ]

            wire (Between(768, 770)) [
                read "craftingGuiOpen"            Bool "CraftingGuiOpen"
                read "craftingFilteringCraftable" Bool "CraftingFilteringCraftable"
                read "smeltingGuiOpen"            Bool "SmeltingGuiOpen"
                read "smeltingFilteringCraftable" Bool "SmeltingFilteringCraftable"
                read "blastGuiOpen"               Bool "BlastGuiOpen"
                read "blastFilteringCraftable"    Bool "BlastFilteringCraftable"
                read "smokerGuiOpen"              Bool "SmokerGuiOpen"
                read "smokerFilteringCraftable"   Bool "SmokerFilteringCraftable"
            ]

            wire (Since 771) [
                read "crafting" (Named "RecipeBookSetting") "Crafting"
                read "furnace"  (Named "RecipeBookSetting") "Furnace"
                read "blast"    (Named "RecipeBookSetting") "Blast"
                read "smoker"   (Named "RecipeBookSetting") "Smoker"
            ]
        }
