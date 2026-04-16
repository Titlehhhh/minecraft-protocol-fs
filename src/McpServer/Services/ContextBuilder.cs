using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace McpServer.Services;

/// <summary>
/// Builds the "TYPES AND IO METHODS" section of the system prompt dynamically
/// based on the protodef kinds actually used in a packet.
///
/// Dictionary maps kind → section file names in Methods/.
/// Empty array [] = structural kind with no IO section (container, void).
/// Missing key → KeyNotFoundException (forces documentation of every new type).
/// </summary>
public static class ContextBuilder
{
    // kind (from ProtodefTypeAnalyzer.GetKindName) → Methods/*.md file names to include
    private static readonly Dictionary<string, string[]> KindToSections =
        new(StringComparer.Ordinal)
        {
            // ── structural (no IO section) ──────────────────────────────────────────
            ["container"] = [],
            ["void"]      = [],

            // ── primitives ─────────────────────────────────────────────────────────
            ["varint"]  = ["Primitives"],
            ["varlong"] = ["Primitives"],
            ["bool"]    = ["Primitives"],
            ["string"]  = ["Primitives"],
            ["pstring"] = ["Primitives"],
            ["uuid"]    = ["Primitives"],
            ["i8"]      = ["Primitives"],
            ["u8"]      = ["Primitives"],
            ["i16"]     = ["Primitives"],
            ["u16"]     = ["Primitives"],
            ["i32"]     = ["Primitives"],
            ["u32"]     = ["Primitives"],
            ["i64"]     = ["Primitives"],
            ["u64"]     = ["Primitives"],
            ["li16"]    = ["Primitives"],
            ["lu16"]    = ["Primitives"],
            ["li32"]    = ["Primitives"],
            ["lu32"]    = ["Primitives"],
            ["li64"]    = ["Primitives"],
            ["lu64"]    = ["Primitives"],
            ["f32"]     = ["Primitives"],
            ["f64"]     = ["Primitives"],
            ["lf32"]    = ["Primitives"],
            ["lf64"]    = ["Primitives"],

            // ── buffer ─────────────────────────────────────────────────────────────
            ["buffer"] = ["Buffer"],

            // ── arrays ─────────────────────────────────────────────────────────────
            ["array"]                    = ["Arrays"],
            ["topBitSetTerminatedArray"] = ["Arrays"],

            // ── switch ─────────────────────────────────────────────────────────────
            ["switch"]     = ["Switch"],
            ["cus_switch"] = ["Switch"],

            // ── mapper ─────────────────────────────────────────────────────────────
            ["mapper"] = ["Mapper"],

            // ── bitfield ───────────────────────────────────────────────────────────
            ["bitfield"]  = ["BitField"],
            ["bitflags"]  = ["BitField"],

            // ── option ─────────────────────────────────────────────────────────────
            ["option"] = ["Option"],

            // ── loop ───────────────────────────────────────────────────────────────
            ["loop"] = ["Loop"],

            // ── registry ───────────────────────────────────────────────────────────
            ["registryEntryHolder"]    = ["RegistryEntryHolder"],
            ["registryEntryHolderSet"] = ["RegistryEntryHolder"],

            // ── nbt ────────────────────────────────────────────────────────────────
            ["nbt"]          = ["Nbt"],
            ["anonymousNbt"] = ["Nbt"],
            ["optionalNbt"]  = ["Nbt"],

            // ── protocol types (registered in ReadType<T>/WriteType<T>) ────────────
            ["position"]    = ["ProtocolTypes"],
            ["vec2f"]       = ["ProtocolTypes"],
            ["vec3f"]       = ["ProtocolTypes"],
            ["vec3f64"]     = ["ProtocolTypes"],
            ["vec3i"]       = ["ProtocolTypes"],
            ["vec4f"]       = ["ProtocolTypes"],

            ["slot"]                    = ["ProtocolTypes"],
            ["hashedSlot"]              = ["ProtocolTypes"],
            ["untrustedSlot"]           = ["ProtocolTypes"],
            ["slotComponent"]           = ["ProtocolTypes"],
            ["untrustedSlotComponent"]  = ["ProtocolTypes"],
            ["slotComponentType"]       = ["ProtocolTypes"],

            ["bannerPattern"]              = ["ProtocolTypes"],
            ["dataComponentMatchers"]      = ["ProtocolTypes"],
            ["exactComponentMatcher"]      = ["ProtocolTypes"],
            ["gameProfile"]                = ["ProtocolTypes"],
            ["itemBlockProperty"]          = ["ProtocolTypes"],
            ["itemEffectDetail"]           = ["ProtocolTypes"],
            ["itemSoundEvent"]             = ["ProtocolTypes"],
            ["minecraftSimpleRecipeFormat"] = ["ProtocolTypes"],
            ["packedChunkPos"]             = ["ProtocolTypes"],
            ["chatSession"]                = ["ProtocolTypes"],
            ["particle"]                   = ["ProtocolTypes"],
            ["particleData"]               = ["ProtocolTypes"],
            ["previousMessages"]           = ["ProtocolTypes"],
            ["serverLinkType"]             = ["ProtocolTypes"],
            ["soundSource"]                = ["ProtocolTypes"],
            ["tags"]                       = ["ProtocolTypes"],

            ["chatType"]               = ["ProtocolTypes"],
            ["chatTypeParameterType"]  = ["ProtocolTypes"],
            ["chatTypes"]              = ["ProtocolTypes"],
            ["chatTypesHolder"]        = ["ProtocolTypes"],
            ["positionUpdateRelatives"] = ["ProtocolTypes"],
            ["recipeBookSetting"]      = ["ProtocolTypes"],
            ["recipeDisplay"]          = ["ProtocolTypes"],
            ["slotDisplay"]            = ["ProtocolTypes"],
            ["movementFlags"]          = ["ProtocolTypes"],

            ["armorTrimMaterial"]              = ["ProtocolTypes"],
            ["armorTrimPattern"]               = ["ProtocolTypes"],
            ["bannerPatternLayer"]             = ["ProtocolTypes"],
            ["entityMetadataPaintingVariant"]  = ["ProtocolTypes"],
            ["entityMetadataWolfVariant"]      = ["ProtocolTypes"],
            ["idSet"]                          = ["ProtocolTypes"],
            ["instrumentData"]                 = ["ProtocolTypes"],
            ["itemBookPage"]                   = ["ProtocolTypes"],
            ["itemConsumeEffect"]              = ["ProtocolTypes"],
            ["itemFireworkExplosion"]           = ["ProtocolTypes"],
            ["itemPotionEffect"]               = ["ProtocolTypes"],
            ["itemSoundHolder"]                = ["ProtocolTypes"],
            ["itemWrittenBookPage"]            = ["ProtocolTypes"],
            ["jukeboxSongData"]                = ["ProtocolTypes"],

            // ── direct extension methods (NOT in ReadType/WriteType) ────────────────
            ["entityMetadataEntry"] = ["DirectExtensions"],
            ["deathLocation"]       = ["DirectExtensions"],
        };

    /// <summary>
    /// Builds the methods context string from a set of protodef kinds.
    /// When <paramref name="dynamic"/> is false, includes all known sections.
    /// Throws <see cref="KeyNotFoundException"/> for any unrecognized kind.
    /// </summary>
    public static string Build(IReadOnlySet<string> kinds, string sectionsFolder, bool dynamic)
    {
        IEnumerable<string> sections = dynamic
            ? SelectSections(kinds)
            : AllSectionNames();

        var sb = new StringBuilder();
        foreach (var section in sections)
        {
            var path = Path.Combine(sectionsFolder, section + ".md");
            sb.AppendLine(File.ReadAllText(path));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static IEnumerable<string> SelectSections(IReadOnlySet<string> kinds)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { "Primitives" };
        yield return "Primitives";

        foreach (var kind in kinds)
        {
            if (!KindToSections.TryGetValue(kind, out var sections))
                throw new KeyNotFoundException(
                    $"Unknown protodef kind '{kind}'. Add it to ContextBuilder.KindToSections.");

            foreach (var section in sections)
                if (seen.Add(section))
                    yield return section;
        }
    }

    private static IEnumerable<string> AllSectionNames()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sections in KindToSections.Values)
            foreach (var s in sections)
                if (seen.Add(s))
                    yield return s;
    }
}
