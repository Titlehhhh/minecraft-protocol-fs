using System.Text;
using ProtoCore;
using Protodef;
using Protodef.Enumerable;
using Protodef.Primitive;

// ── Config ────────────────────────────────────────────────────────────────────
string outPath = args.Length > 0
    ? args[0]
    : Path.Combine(Directory.GetCurrentDirectory(), "type-examples.md");

const int MaxSchemaChars  = 1500;   // max chars per JSON schema block
const int MaxPacketsPerKind = 25;   // max packets shown per type kind
const int MaxFieldsPerPacket = 6;   // max fields shown per (packet, kind) entry
const int MaxSchemasPerField = 2;   // max schema variants per field entry

// ── Skip these primitive kinds ────────────────────────────────────────────────
static bool IsPrimitive(string k) => k is
    "varint" or "varlong" or "bool" or "void" or "string" or "pstring" or
    "i8"    or "u8"    or "i16" or "u16" or "i32" or "u32" or
    "i64"   or "u64"   or "f32" or "f64" or
    "li8"   or "lu8"   or "li16" or "lu16" or
    "li32"  or "lu32"  or "li64" or "lu64" or "lf32" or "lf64";

// ── Load ──────────────────────────────────────────────────────────────────────
Console.Error.WriteLine("Loading minecraft-data protocols…");
var map = await ProtocolLoader.LoadProtocolsAsync(47, 9999);
int minVer = map.Protocols.Keys.Min();
int maxVer = map.Protocols.Keys.Max();
Console.Error.WriteLine($"  {map.Protocols.Count} versions: {minVer}–{maxVer}");

// Index: (packetFullPath, fieldPath, kindName) → { schemaJson → SortedSet<version> }
var index = new Dictionary<(string pkt, string fld, string kind),
                            Dictionary<string, SortedSet<int>>>();

int totalScanned = 0;

foreach (var (ver, info) in map.Protocols)
{
    if (info.Protocol is null) continue;

    foreach (var ns in info.Protocol.EnumerateNamespaces())
    {
        foreach (var (typeName, rootNode) in ns.Types)
        {
            if (rootNode is null) continue;
            totalScanned++;

            string pktKey = $"{ns.Fullname}.{typeName}";

            foreach (var (fieldPath, complexNode) in ScanComplex(rootNode, ""))
            {
                string kind;
                try   { kind = ProtodefTypeAnalyzer.GetKindName(complexNode); }
                catch { continue; }

                if (IsPrimitive(kind)) continue;

                var entryKey = (pktKey, fieldPath, kind);
                if (!index.TryGetValue(entryKey, out var schemaMap))
                    index[entryKey] = schemaMap = [];

                string schema;
                try   { schema = Truncate(complexNode.ToJson(), MaxSchemaChars); }
                catch { schema = $"(serialization error: {complexNode.GetType().Name})"; }

                if (!schemaMap.TryGetValue(schema, out var vset))
                    schemaMap[schema] = vset = [];
                vset.Add(ver);
            }
        }
    }
}

Console.Error.WriteLine($"  Scanned {totalScanned} type-version pairs");
Console.Error.WriteLine($"  Indexed {index.Count} (packet × field × kind) entries");

// ── Render ────────────────────────────────────────────────────────────────────
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
using var md = new StreamWriter(outPath, append: false, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

void L(string s = "") => md.WriteLine(s);

L("# Protodef — примеры сложных типов");
L();
L($"> Сгенерировано: {DateTime.UtcNow:yyyy-MM-dd} UTC");
L($"> minecraft-data версии: **{minVer}–{maxVer}** ({map.Protocols.Count} шт.)");
L();
L("## Содержание");
L();

// Collect kind stats for TOC
var byKind = index
    .GroupBy(kvp => kvp.Key.kind)
    .Select(g => (kind: g.Key, packets: g.GroupBy(x => x.Key.pkt).Count()))
    .OrderBy(x => x.kind)
    .ToList();

foreach (var (kind, pktCount) in byKind)
{
    string anchor = kind.ToLowerInvariant().Replace(" ", "-");
    L($"- [`{kind}`](#{anchor}) — {pktCount} пакетов");
}
L();
L("---");
L();

// Main content
foreach (var (kind, _) in byKind)
{
    var kindEntries = index.Where(kvp => kvp.Key.kind == kind).ToList();
    var packetGroups = kindEntries
        .GroupBy(kvp => kvp.Key.pkt)
        .OrderBy(g => g.Key)
        .ToList();

    L($"## `{kind}`");
    L();
    L($"Встречается в **{packetGroups.Count}** пакетах.");
    L();

    int shown = 0;
    foreach (var pktGroup in packetGroups)
    {
        if (shown >= MaxPacketsPerKind) break;
        string pkt = pktGroup.Key;

        L($"### {pkt}");
        L();

        int fieldCount = 0;
        foreach (var entry in pktGroup.OrderBy(e => e.Key.fld))
        {
            if (fieldCount >= MaxFieldsPerPacket) break;

            string fld = entry.Key.fld;
            if (!string.IsNullOrEmpty(fld))
                L($"**Поле:** `{fld}`  ");

            int schemaCount = 0;
            foreach (var (schema, vset) in entry.Value.OrderBy(x => x.Value.Min()))
            {
                if (schemaCount >= MaxSchemasPerField) break;
                L($"**Версии:** `{VerRange(vset)}`");
                L("```json");
                L(schema);
                L("```");
                L();
                schemaCount++;
            }
            fieldCount++;
        }

        shown++;
    }

    if (packetGroups.Count > MaxPacketsPerKind)
        L($"> *…ещё {packetGroups.Count - MaxPacketsPerKind} пакетов не показано*");

    L("---");
    L();
}

Console.Error.WriteLine($"✅ Записано: {outPath}");

// ── Helpers ───────────────────────────────────────────────────────────────────

/// <summary>
/// Recursively yields all complex (non-root) nodes with their dot-path within the type tree.
/// </summary>
static IEnumerable<(string path, ProtodefType node)> ScanComplex(ProtodefType t, string p)
{
    // Don't yield the root node itself (empty path) — the packet type IS a container,
    // we only care about its children
    if (!string.IsNullOrEmpty(p))
        yield return (p, t);

    foreach (var (key, child) in t.Children)
    {
        string cp = string.IsNullOrEmpty(p)
            ? (key ?? "_")
            : $"{p}.{key ?? "_"}";

        foreach (var r in ScanComplex(child, cp))
            yield return r;
    }
}

static string VerRange(SortedSet<int> vs)
{
    int lo = vs.Min, hi = vs.Max;
    bool contig = vs.Count == hi - lo + 1;
    return contig
        ? $"{lo}–{hi}"
        : $"{lo}–{hi} (прерывисто, {vs.Count} версий)";
}

static string Truncate(string s, int max) =>
    s.Length <= max ? s : s[..max] + "\n… (усечено)";
