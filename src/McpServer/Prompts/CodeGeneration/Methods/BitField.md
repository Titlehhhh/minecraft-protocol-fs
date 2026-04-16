## BitField

A `["bitfield", [...nodes]]` packs multiple named values into a single integer.
Each node: `{"name": "x", "size": N, "signed": bool}`.

**Bit order:** the LAST node in the JSON array = Least Significant Bits (LSB).

**Pattern:** read the base integer, then extract each field with shifts and masks.
The base integer size = sum of all bit sizes (e.g. 26+12+26=64 → i64).

```csharp
// Schema: [{name:"x",size:26,signed:true},{name:"y",size:12,signed:true},{name:"z",size:26,signed:true}]
// Total: 64 bits → read i64

// Read:
long raw = reader.ReadSignedLong();
X = (int)(raw >> 38);                         // top 26 bits
Y = (int)((raw >> 26) & 0xFFF);              // middle 12 bits
Z = (int)(raw & 0x3FFFFFF);                  // bottom 26 bits
// Sign-extend if signed:
X = X >= (1 << 25) ? X - (1 << 26) : X;

// Write:
long raw = ((long)(X & 0x3FFFFFF) << 38) | ((long)(Y & 0xFFF) << 26) | (long)(Z & 0x3FFFFFF);
writer.WriteSignedLong(raw);
```

**Base type selection:**
- Total bits ≤ 8  → u8  (ReadUnsignedByte)
- Total bits ≤ 16 → u16 (ReadUnsignedShort)
- Total bits ≤ 32 → i32 (ReadSignedInt)
- Total bits ≤ 64 → i64 (ReadSignedLong)
