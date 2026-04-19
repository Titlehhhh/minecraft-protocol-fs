## Protocol-Specific Types (ReadType<T> / WriteType<T>)

Registered in `Extensions/ProtocolSerializationExtensions.cs`.
Usage: `reader.ReadType<T>(protocolVersion)` / `writer.WriteType<T>(value, protocolVersion)`.

**⚠️ CRITICAL — protodef type name → C# type name mapping:**
Always use the C# type from the right column, REGARDLESS of the field name.

| protodef type | C# type |
|---|---|
| `position` | `Position` |
| `vec2f` | `Vec2f` |
| `vec3f` | `Vec3f` |
| `vec3f64` | `Vec3f64` |
| `vec3i` | `Vec3i` |
| `vec4f` | `Vec4f` |

❌ WRONG: field `{"name": "location", "type": "position"}` → `WriteType<Location>` (named after field!)
✅ CORRECT: → `WriteType<Position>(Location, protocolVersion)`

**Geometric types:** Position, Vec2f, Vec3f, Vec3f64, Vec3i, Vec4f

**Slot family:** Slot, HashedSlot, UntrustedSlot, SlotComponent, UntrustedSlotComponent, SlotComponentType

**Common types:** BannerPattern, DataComponentMatchers, ExactComponentMatcher, GameProfile,
ItemBlockProperty, ItemEffectDetail, ItemSoundEvent, MinecraftSimpleRecipeFormat,
PackedChunkPos, ChatSession, Particle, ParticleData, PreviousMessages, ServerLinkType,
SoundSource, Tags, EntityMetadataEntry, DeathLocation

**Slot component dependencies:** ArmorTrimMaterial, ArmorTrimPattern, BannerPatternLayer,
EntityMetadataPaintingVariant, EntityMetadataWolfVariant, IDSet, InstrumentData,
ItemBlockPredicate, ItemBookPage, ItemConsumeEffect, ItemFireworkExplosion,
ItemPotionEffect, ItemSoundHolder, ItemWrittenBookPage, JukeboxSongData

**Play helper types:** ChatType, ChatTypeParameterType, ChatTypes, ChatTypesHolder,
PositionUpdateRelatives, RecipeBookSetting, RecipeDisplay, SlotDisplay, MovementFlags
