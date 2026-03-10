## Core Primitives (MinecraftPrimitiveReader/Writer)

Schema type → C# method:

- varint → ReadVarInt / WriteVarInt          (variable-length int32, MOST COMMON for integers)
- varlong → ReadVarLong / WriteVarLong
- i8 → ReadSignedByte / WriteSignedByte
- u8 → ReadUnsignedByte / WriteUnsignedByte
- i16 → ReadSignedShort / WriteSignedShort
- u16 → ReadUnsignedShort / WriteUnsignedShort
- i32 → ReadSignedInt / WriteSignedInt     (fixed 4-byte int, rare)
- u32 → ReadUnsignedInt / WriteUnsignedInt
- i64 → ReadSignedLong / WriteSignedLong
- u64 → ReadUnsignedLong / WriteUnsignedLong
- f32 → ReadFloat / WriteFloat
- f64 → ReadDouble / WriteDouble
- string → ReadString / WriteString
- UUID → ReadUUID / WriteUUID
- bool → ReadBoolean / WriteBoolean
- buffer → ReadBuffer(len), ReadRestBuffer, WriteBuffer(ReadOnlySpan<byte>)

## Array Helpers

- ReadExtensions:
    - ReadArray<T>(LengthFormat, ReadDelegate<T>)
    - ReadArray<T, TReader>(LengthFormat[, protocolVersion]) for SIMD/readers
- ProtocolSerializationExtensions:
    - WriteArray<T>(ReadOnlySpan<T>[, LengthFormat][, protocolVersion])
    - ReadArray<T>(LengthFormat[, protocolVersion])

## Protocol-Specific Types (ReadType<T>/WriteType<T>)

These are registered in `Extensions/ProtocolSerializationExtensions.cs` and routed via
`ReadType<T>(protocolVersion)` / `WriteType<T>(value, protocolVersion)`.

**Geometric types:**

- Position, Vec2f, Vec3f, Vec3f64, Vec3i, Vec4f

**Slot family:**

- Slot, HashedSlot, UntrustedSlot, SlotComponent, UntrustedSlotComponent, SlotComponentType

**Common types:**

- BannerPattern, DataComponentMatchers, ExactComponentMatcher, GameProfile,
  ItemBlockProperty, ItemEffectDetail, ItemSoundEvent, MinecraftSimpleRecipeFormat,
  PackedChunkPos, ChatSession, Particle, ParticleData, PreviousMessages, ServerLinkType,
  SoundSource, Tags

**Slot component dependencies:**

- ArmorTrimMaterial, ArmorTrimPattern, BannerPatternLayer,
  EntityMetadataPaintingVariant, EntityMetadataWolfVariant, IDSet, InstrumentData,
  ItemBlockPredicate, ItemBookPage, ItemConsumeEffect, ItemFireworkExplosion,
  ItemPotionEffect, ItemSoundHolder, ItemWrittenBookPage, JukeboxSongData

**Entity metadata:**

- EntityMetadataEntry

**Packet common payloads:**

- PacketCommonAddResourcePack, PacketCommonClearDialog,
  PacketCommonCookieRequest, PacketCommonCookieResponse, PacketCommonCustomClickAction,
  PacketCommonCustomReportDetails, PacketCommonRemoveResourcePack,
  PacketCommonSelectKnownPacks, PacketCommonServerLinks, PacketCommonSettings,
  PacketCommonStoreCookie, PacketCommonTransfer

**Play helper types:**

- ChatType, ChatTypeParameterType, ChatTypes, ChatTypesHolder,
  PositionUpdateRelatives, RecipeBookSetting, RecipeDisplay, SlotDisplay, MovementFlags

**Death location:**

- DeathLocation

## RegistryEntryHolder<T>

Supported value types are registered in `ProtocolSerializationExtensions.cs`:

- string, int, ArmorTrimMaterial, ArmorTrimPattern, BannerPattern,
  EntityMetadataPaintingVariant, EntityMetadataWolfVariant, InstrumentData,
  ItemSoundEvent, JukeboxSongData

## NBT Helpers

From `ProtocolSerializationExtensions.Nbt.cs`. All methods require `protocolVersion` argument.
Return/field type: `NbtTag` (requires `using McProtoNet.NBT;` in the packet file).

- `ReadNbtTag(protocolVersion)` / `WriteNbtTag(NbtTag value, protocolVersion)`
- `ReadOptionalNbtTag(protocolVersion)` / `WriteOptionalNbtTag(NbtTag? value, protocolVersion)`
- `ReadAnonymousNbtTag(protocolVersion)` / `WriteAnonymousNbtTag(NbtTag value, protocolVersion)`
- `ReadAnonOptionalNbtTag(protocolVersion)` / `WriteAnonOptionalNbtTag(NbtTag? value, protocolVersion)`

When using NBT, add `using McProtoNet.NBT;` as the first line before `{{usages}}`.

## container_id field (protocol 766+)

Do NOT invent a type called `ContainerID`. Use the actual primitive based on schema:
- `u8` → `ReadUnsignedByte / WriteUnsignedByte`, C# type `byte`
- `i16` → `ReadSignedShort / WriteSignedShort`, C# type `short`
- `varint` → `ReadVarInt / WriteVarInt`, C# type `int`