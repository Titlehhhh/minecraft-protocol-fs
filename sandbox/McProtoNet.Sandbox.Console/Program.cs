using McProtoNet.Protocol;
using McProtoNet.Serialization;

int version = MinecraftVersion.LatestProtocol;

static string Hex(byte[] b) => Convert.ToHexString(b);

Console.WriteLine($"protocol version = {version}\n");

// --- Vec3f: plain numeric record struct ---
{
    var v = new Vec3f(1.5f, -2.0f, 3.25f);
    var w = new MinecraftPrimitiveWriter();
    v.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = Vec3f.Read(ref r, version);
    Console.WriteLine($"Vec3f   {v}");
    Console.WriteLine($"  wire  {Hex(bytes)}  ({bytes.Length} bytes)");
    Console.WriteLine($"  read  {back}   round-trips: {v == back}\n");
}

// --- Vec3i: varint-encoded ---
{
    var v = new Vec3i(300, -1, 42);
    var w = new MinecraftPrimitiveWriter();
    v.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = Vec3i.Read(ref r, version);
    Console.WriteLine($"Vec3i   {v}");
    Console.WriteLine($"  wire  {Hex(bytes)}  ({bytes.Length} bytes)");
    Console.WriteLine($"  read  {back}   round-trips: {v == back}\n");
}

// --- MovementFlags: u8 bitflags ---
{
    var f = new MovementFlags(OnGround: true, HasHorizontalCollision: false);
    var w = new MinecraftPrimitiveWriter();
    f.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = MovementFlags.Read(ref r, version);
    Console.WriteLine($"MovementFlags {f}");
    Console.WriteLine($"  wire  {Hex(bytes)}   read {back}   round-trips: {f == back}\n");
}

// --- PositionUpdateRelatives: version-dependent bitflags (u32 @ 772, u8 @ 766) ---
{
    var rel = new PositionUpdateRelatives(X: true, Y: false, Z: true, Yaw: false, Pitch: false,
                                          Dx: true, Dy: false, Dz: false, YawDelta: true);
    var w = new MinecraftPrimitiveWriter();
    rel.Write(w, 772);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = PositionUpdateRelatives.Read(ref r, 772);
    Console.WriteLine($"PositionUpdateRelatives @772 (u32) {rel}");
    Console.WriteLine($"  wire  {Hex(bytes)}   read {back}   round-trips: {rel == back}");

    // at 766 the wire is a single u8 (only x,y,z,yaw,pitch); dx/dy/dz/yawDelta are dropped.
    var w2 = new MinecraftPrimitiveWriter();
    rel.Write(w2, 766);
    var bytes2 = w2.ToArray();
    Console.WriteLine($"  @766 (u8) wire {Hex(bytes2)}  ({bytes2.Length} byte)\n");
}

Console.WriteLine("done — poke away.");
