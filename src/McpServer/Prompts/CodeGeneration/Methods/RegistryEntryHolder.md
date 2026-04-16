## RegistryEntryHolder<T> — Registry from server

Used when field schema type is: `["registryEntryHolder", { "baseName": "...", "otherwise": ... }]`

- `reader.ReadRegistryEntryHolder<T>(protocolVersion)` → `RegistryEntryHolder<T>`
- `writer.WriteRegistryEntryHolder<T>(RegistryEntryHolder<T> value, protocolVersion)`

**⚠️ CRITICAL:** The `"baseName"` is the **registry NAME, NOT a C# type!**

**How to determine T:**
1. Look at schema structure, field name, and `baseName`
2. Use ONLY supported value types (below)
3. If unsure → default to `RegistryEntryHolder<string>`

**Supported value types:** string, int, ArmorTrimMaterial, ArmorTrimPattern, BannerPattern,
EntityMetadataPaintingVariant, EntityMetadataWolfVariant, InstrumentData, ItemSoundEvent, JukeboxSongData

**Example:**
```json
{"name": "dialog", "type": ["registryEntryHolder", {"baseName": "dialog", "otherwise": {...}}]}
```
✅ CORRECT:
```csharp
public RegistryEntryHolder<string> Dialog { get; set; }
writer.WriteRegistryEntryHolder<string>(Dialog, protocolVersion);
Dialog = reader.ReadRegistryEntryHolder<string>(protocolVersion);
```
❌ WRONG — `"baseName": "dialog"` does NOT mean use type `Dialog` (it doesn't exist!):
```csharp
public RegistryEntryHolder<Dialog> Dialog { get; set; }
```
