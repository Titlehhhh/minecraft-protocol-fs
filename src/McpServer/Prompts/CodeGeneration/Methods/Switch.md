## Protodef `switch` — heterogeneous fields

A protodef `["switch", {"compareTo": "fieldName", "fields": {"val": type, ...}}]` means
the field type depends on the value of another field in the same container.

**Pattern:**
1. Declare one **nullable** property per case value on the class.
2. In Serialize/Deserialize: switch on the determining field, read/write the matching nullable.

**Example schema:**
```
action: varint
actionData: switch(compareTo: action) {
  0: StartFields(x: f64, y: f64, z: f64)
  1: StopFields(reason: string)
}
```

**Generated C# pattern:**
```csharp
public int Action { get; set; }
public StartFieldsData? ActionDataStart { get; set; }
public StopFieldsData? ActionDataStop { get; set; }

public struct StartFieldsData { public double X, Y, Z; }
public struct StopFieldsData  { public string Reason; }

// Serialize:
writer.WriteVarInt(Action);
switch (Action)
{
    case 0: { var f = ActionDataStart ?? throw new InvalidOperationException(...); writer.WriteDouble(f.X); ... break; }
    case 1: { var f = ActionDataStop  ?? throw new InvalidOperationException(...); writer.WriteString(f.Reason); break; }
}

// Deserialize:
Action = reader.ReadVarInt();
switch (Action)
{
    case 0: ActionDataStart = new StartFieldsData { X = reader.ReadDouble(), ... }; break;
    case 1: ActionDataStop  = new StopFieldsData  { Reason = reader.ReadString() }; break;
}
```

**If a case type is `void`** — that case has no payload: empty case body, no struct needed.

**`default` case in schema** — represents "all other values". Generate a `default:` branch.
