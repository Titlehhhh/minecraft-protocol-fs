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

// --- MinecartStep: nested named types via ReadType<Vec3f>/WriteType<Vec3f> ---
{
    var step = new MinecartStep(new Vec3f(1f, 2f, 3f), new Vec3f(-0.5f, 0f, 0.5f), 90f, -45f, 1f);
    var w = new MinecraftPrimitiveWriter();
    step.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = MinecartStep.Read(ref r, version);
    var same = step.Position == back.Position && step.Movement == back.Movement
               && step.Yaw == back.Yaw && step.Pitch == back.Pitch && step.Weight == back.Weight;
    Console.WriteLine($"MinecartStep pos={step.Position} mov={step.Movement}");
    Console.WriteLine($"  wire  {Hex(bytes)}  ({bytes.Length} bytes)   round-trips: {same}\n");
}

// --- ExplosionBlockOffset: i8 triple (write narrows int -> sbyte) ---
{
    var off = new ExplosionBlockOffset(-1, 2, -3);
    var w = new MinecraftPrimitiveWriter();
    off.Write(w, 754);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = ExplosionBlockOffset.Read(ref r, 754);
    Console.WriteLine($"ExplosionBlockOffset {off}");
    Console.WriteLine($"  wire  {Hex(bytes)}  ({bytes.Length} bytes)   round-trips: {off == back}\n");
}

// --- SetProtocolPacket: first generated packet (handshake) ---
{
    var hs = new SetProtocolPacket(772, "mc.example.org", 25565, 2);
    var w = new MinecraftPrimitiveWriter();
    hs.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = SetProtocolPacket.Read(ref r, version);
    var same = hs.ProtocolVersion == back.ProtocolVersion && hs.ServerHost == back.ServerHost
               && hs.ServerPort == back.ServerPort && hs.NextState == back.NextState;
    Console.WriteLine($"SetProtocolPacket v{hs.ProtocolVersion} {hs.ServerHost}:{hs.ServerPort} next={hs.NextState}");
    Console.WriteLine($"  wire  {Hex(bytes)}  ({bytes.Length} bytes)   round-trips: {same}\n");
}

// --- UpdateTimePacket: MULTIVERSION — 2 fields @763, 3 fields @772 ---
{
    var pkt = new UpdateTimePacket(6000, 13000, true);
    foreach (var v in new[] { 763, 772 })
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = UpdateTimePacket.Read(ref r, v);
        Console.WriteLine($"UpdateTimePacket @{v}: {bytes.Length} bytes, age/time ok: {back.Age == 6000 && back.Time == 13000}, tickDayTime={back.TickDayTime}");
    }
    Console.WriteLine();
}

// --- LoginStartPacket: MULTIVERSION — 5 wire layouts across 758..772 ---
{
    var pkt = new LoginStartPacket("Steve", null, Guid.NewGuid());
    foreach (var v in new[] { 758, 761, 772 })
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = LoginStartPacket.Read(ref r, v);
        Console.WriteLine($"LoginStartPacket @{v}: {bytes.Length} bytes, name={back.Username}, uuid roundtrip: {back.PlayerUuid == (v >= 760 ? pkt.PlayerUuid : null)}");
    }
    Console.WriteLine();
}

// --- DamageEventPacket: Option<Vec3f64> both present and absent ---
{
    var with_ = new DamageEventPacket(7, 1, 2, 3, new Vec3f64(1.5, 2.5, 3.5));
    var without = new DamageEventPacket(7, 1, 2, 3, null);
    foreach (var pkt in new[] { with_, without })
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, 772);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = DamageEventPacket.Read(ref r, 772);
        Console.WriteLine($"DamageEventPacket pos={pkt.SourcePosition?.ToString() ?? "null"}: {bytes.Length} bytes, roundtrip: {back.SourcePosition == pkt.SourcePosition}");
    }
    Console.WriteLine();
}

// --- MoveMinecartPacket: array of nested MinecartStep ---
{
    var steps = new[]
    {
        new MinecartStep(new Vec3f(1, 2, 3), new Vec3f(0.1f, 0, -0.1f), 10f, 20f, 1f),
        new MinecartStep(new Vec3f(4, 5, 6), new Vec3f(-0.2f, 0, 0.2f), 30f, 40f, 2f),
    };
    var pkt = new MoveMinecartPacket(42, steps);
    var w = new MinecraftPrimitiveWriter();
    pkt.Write(w, 772);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = MoveMinecartPacket.Read(ref r, 772);
    Console.WriteLine($"MoveMinecartPacket: {bytes.Length} bytes, {back.Steps.Length} steps, step[1].Position roundtrip: {back.Steps[1].Position == steps[1].Position}\n");
}

// --- LoginPluginRequestPacket: restBuffer (read to end) ---
{
    var pkt = new LoginPluginRequestPacket(5, "fml:handshake", new byte[] { 1, 2, 3, 4, 5 });
    var w = new MinecraftPrimitiveWriter();
    pkt.Write(w, 772);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = LoginPluginRequestPacket.Read(ref r, 772);
    Console.WriteLine($"LoginPluginRequestPacket: {bytes.Length} bytes, channel={back.Channel}, data.Length={back.Data.Length}\n");
}

// --- PlayerPositionPacket: 5 wire layouts — raw i8 flags era, named bitflags era, 768 reorder ---
{
    var flags = new PositionUpdateRelatives(X: true, Y: false, Z: true, Yaw: false, Pitch: false,
                                            Dx: false, Dy: false, Dz: false, YawDelta: false);
    var pkt = new PlayerPositionPacket(100.5, 64.0, -200.25, 90f, -10f, flags, 7, false, 0, 0, 0);
    foreach (var v in new[] { 754, 758, 766, 772 })
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = PlayerPositionPacket.Read(ref r, v);
        var ok = back.X == pkt.X && back.Yaw == pkt.Yaw && back.TeleportId == pkt.TeleportId
                 && back.Flags.X == flags.X && back.Flags.Z == flags.Z;
        Console.WriteLine($"PlayerPositionPacket @{v}: {bytes.Length} bytes, roundtrip: {ok}");
    }
    Console.WriteLine();
}

// --- SpawnInfo (766 vs 768: +seaLevel) with optional DeathLocation ---
{
    var death = new DeathLocation("minecraft:overworld", new Position(10, 64, -20));
    var info = new SpawnInfo(2, "minecraft:overworld", 12345L, 1, 0, false, true, death, 3, 63);
    foreach (var v in new[] { 766, 772 })
    {
        var w = new MinecraftPrimitiveWriter();
        info.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = SpawnInfo.Read(ref r, v);
        var ok = back.Name == info.Name && back.Death!.Location == death.Location
                 && back.SeaLevel == (v >= 768 ? 63 : 0);
        Console.WriteLine($"SpawnInfo @{v}: {bytes.Length} bytes, roundtrip: {ok}, death={back.Death?.Location}");
    }
    Console.WriteLine();
}

// --- SetCooldownPacket: api morph — ItemId era vs CooldownGroup era ---
{
    var pkt = new SetCooldownPacket(42, "minecraft:ender_pearl", 100);
    foreach (var v in new[] { 767, 772 })
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = SetCooldownPacket.Read(ref r, v);
        Console.WriteLine($"SetCooldownPacket @{v}: {bytes.Length} bytes, ticks={back.CooldownTicks}, item={back.ItemId}, group={back.CooldownGroup ?? "-"}");
    }
    Console.WriteLine();
}

Console.WriteLine("done — poke away.");
