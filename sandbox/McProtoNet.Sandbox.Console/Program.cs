using McProtoNet.Protocol;
using McProtoNet.Protocol.Packets.Handshaking.Serverbound;
using McProtoNet.Protocol.Packets.Login.Clientbound;
using McProtoNet.Protocol.Packets.Login.Serverbound;
using McProtoNet.Protocol.Packets.Play.Clientbound;
using McProtoNet.Protocol.Packets.Status.Clientbound;
using McProtoNet.Protocol.Packets.Status.Serverbound;
using McProtoNet.Serialization;
using McProtoNet.NBT;

int version = MinecraftVersion.LatestProtocol;

static string Hex(byte[] b) => Convert.ToHexString(b);

static void Assert(bool condition, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string? expr = null)
{
    if (!condition) throw new Exception($"assertion failed: {expr}");
}

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

// --- LpVec3: hand-written runtime primitive, quantized with a transmitted scale (773+) ---
{
    var zero = new LpVec3(0d, 0d, 0d);
    var wz = new MinecraftPrimitiveWriter();
    zero.Write(wz, version);
    var zeroBytes = wz.ToArray();
    Assert(Hex(zeroBytes) == "00");

    // Both vectors and their wire bytes come from the protocol documentation's own samples.
    foreach (var (v, expected) in new[]
             {
                 (new LpVec3(1.0d, 0.0d, -1.0d), "F1FF0000FFFF"),
                 (new LpVec3(10.0d, 0.2d, -5.0d), "F6FF4001051F02"),
             })
    {
        var w = new MinecraftPrimitiveWriter();
        v.Write(w, version);
        var bytes = w.ToArray();
        Assert(Hex(bytes) == expected);

        var r = new MinecraftPrimitiveReader(bytes);
        var back = LpVec3.Read(ref r, version);
        double step = Math.Ceiling(Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)))) * 2d / 32766d;
        Assert(Math.Abs(back.X - v.X) <= step && Math.Abs(back.Y - v.Y) <= step && Math.Abs(back.Z - v.Z) <= step);

        Console.WriteLine($"LpVec3  {v}");
        Console.WriteLine($"  wire  {Hex(bytes)}  ({bytes.Length} bytes)");
        Console.WriteLine($"  read  {back}");
        Console.WriteLine();
    }

    var rng = new Random(776);
    for (int i = 0; i < 2000; i++)
    {
        var v = new LpVec3(
            (rng.NextDouble() - 0.5d) * Math.Pow(10d, rng.Next(-4, 6)),
            (rng.NextDouble() - 0.5d) * Math.Pow(10d, rng.Next(-4, 6)),
            (rng.NextDouble() - 0.5d) * Math.Pow(10d, rng.Next(-4, 6)));

        var w = new MinecraftPrimitiveWriter();
        v.Write(w, version);
        var bytes = w.ToArray();
        Assert(bytes.Length == 1 || bytes.Length >= 6);

        var r = new MinecraftPrimitiveReader(bytes);
        var back = LpVec3.Read(ref r, version);
        double scale = Math.Ceiling(Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z))));
        double step = Math.Max(scale, 1d) * 2d / 32766d;
        Assert(Math.Abs(back.X - v.X) <= step && Math.Abs(back.Y - v.Y) <= step && Math.Abs(back.Z - v.Z) <= step);
    }

    Console.WriteLine("LpVec3  2000 random vectors round-trip within one quantization step");
    Console.WriteLine();
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

// --- UpdateTimePacket: MULTIVERSION - time+tickDayTime through 774, clock list from 775 ---
{
    var cases = new (int Version, UpdateTimePacket Packet)[]
    {
        (763, new UpdateTimePacket(6000, VUntil767: new(13000))),
        (772, new UpdateTimePacket(6000, V768_774: new(13000, true))),
        (776, new UpdateTimePacket(6000, V775_Last: new(new[] { new ClockUpdate(0, 13000, 0.25f, 1f) }))),
    };

    foreach (var (v, pkt) in cases)
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = UpdateTimePacket.Read(ref r, v);
        Assert(back.Age == 6000);
        Console.WriteLine($"UpdateTimePacket @{v}: {bytes.Length} bytes, clocks={back.V775_Last?.ClockUpdates.Length.ToString() ?? "-"}, tickDayTime={back.V768_774?.TickDayTime.ToString() ?? "-"}");
    }
    Console.WriteLine();
}

// --- LoginStartPacket: MULTIVERSION — 5 wire layouts across 758..772 ---
{
    var uuid = Guid.NewGuid();
    var cases = new (int Version, LoginStartPacket Packet, Guid? ExpectedUuid)[]
    {
        (758, new LoginStartPacket("Steve"), null),
        (761, new LoginStartPacket("Steve", V761_763: new(uuid)), uuid),
        (772, new LoginStartPacket("Steve", V764_Last: new(uuid)), uuid),
    };
    foreach (var (v, pkt, expectedUuid) in cases)
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = LoginStartPacket.Read(ref r, v);
        Guid? backUuid = v switch
        {
            761 => back.V761_763?.PlayerUuid,
            >= 764 => back.V764_Last!.Value.PlayerUuid,
            _ => null
        };
        Console.WriteLine($"LoginStartPacket @{v}: {bytes.Length} bytes, name={back.Username}, uuid roundtrip: {backUuid == expectedUuid}");
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
    var pkt = new PlayerPositionPacket(100.5, 64.0, -200.25, 90f, -10f, flags, 7,
        V755_761: new(false), V768_Last: new(0, 0, 0));
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
    var pkt = new SetCooldownPacket(100, VUntil767: new(42), V768_Last: new("minecraft:ender_pearl"));
    foreach (var v in new[] { 767, 772 })
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = SetCooldownPacket.Read(ref r, v);
        Console.WriteLine($"SetCooldownPacket @{v}: {bytes.Length} bytes, ticks={back.CooldownTicks}, item={back.VUntil767?.ItemId.ToString() ?? "-"}, group={back.V768_Last?.CooldownGroup ?? "-"}");
    }
    Console.WriteLine();
}

// --- SpawnPositionPacket: MULTIVERSION - position @754, +angle @755, RespawnData @773 ---
{
    var loc = new Position(10, 64, -20);
    var cases = new (int Version, SpawnPositionPacket Packet)[]
    {
        (754, new SpawnPositionPacket(VUntil754: new(loc))),
        (772, new SpawnPositionPacket(V755_772: new(loc, 90f))),
        (776, new SpawnPositionPacket(V773_Last: new(new RespawnData(new GlobalPos("minecraft:overworld", loc), 90f, -12.5f)))),
    };

    foreach (var (v, pkt) in cases)
    {
        var w = new MinecraftPrimitiveWriter();
        pkt.Write(w, v);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = SpawnPositionPacket.Read(ref r, v);
        var readLoc = back.VUntil754?.Location ?? back.V755_772?.Location ?? back.V773_Last!.Value.RespawnData.GlobalPos.Location;
        Assert(readLoc == loc);
        Console.WriteLine($"SpawnPositionPacket @{v}: {bytes.Length} bytes, loc={readLoc}");
    }
    Assert(SpawnPositionPacket.GetPacketId(735) == 0x42);
    Assert(SpawnPositionPacket.GetPacketId(772) == 0x5A);
    Console.WriteLine();
}

// --- TeamAction: a dunet union behind TeamsPacket — string era @764, nbt era @772 ---
{
    // the mode field is wire-only: the model carries the case, the packet derives the byte
    var created = new TeamAction.CreatedVUntil764(
        "Reds", 1, "always", "never", 4, "[", "]", new[] { "Steve", "Alex" });
    Assert(created.Discriminator(764) == 0);

    var w = new MinecraftPrimitiveWriter();
    new TeamsPacket("reds", created).Write(w, 764);
    var bytes = w.ToArray();
    // "reds" is one length byte plus four chars, so the mode byte sits at index 5
    Assert(bytes[5] == 0x00);

    var r = new MinecraftPrimitiveReader(bytes);
    var back = TeamsPacket.Read(ref r, 764);
    Assert(back.TeamName == "reds");
    var backCreated = back.Action as TeamAction.CreatedVUntil764;
    Assert(backCreated is not null);
    Assert(backCreated!.Name == "Reds" && backCreated.FriendlyFire == 1 && backCreated.Prefix == "["
           && backCreated.Players.Length == 2 && backCreated.Players[1] == "Alex");
    Console.WriteLine($"TeamsPacket @764: {bytes.Length} bytes, mode={bytes[5]}, case={back.Action.GetType().Name}");

    // one arm, two keys: the read accepts 3 and 4, the write picks the first
    var changed = new TeamAction.PlayersChanged(new[] { "Steve" });
    Assert(changed.Discriminator(772) == 3);
    var w2 = new MinecraftPrimitiveWriter();
    new TeamsPacket("reds", changed).Write(w2, 772);
    var changedBytes = w2.ToArray();
    Assert(changedBytes[5] == 0x03);
    changedBytes[5] = 0x04;
    var r2 = new MinecraftPrimitiveReader(changedBytes);
    var back2 = TeamsPacket.Read(ref r2, 772);
    Assert(back2.Action is TeamAction.PlayersChanged { Players.Length: 1 });
    Console.WriteLine($"TeamsPacket @772: modes 3 and 4 both read as {back2.Action.GetType().Name}");

    // the 771 layer swaps the text fields to nbt, so Created is a second case, not the same one
    var teamFlags = new TeamFlags(FriendlyFire: true, SeeFriendlyInvisible: false);
    var created772 = new TeamAction.CreatedV771_Last(
        new NbtCompound().With("text", new NbtString("Reds")), teamFlags, 0, 1, 4,
        new NbtCompound().With("text", new NbtString("[")),
        new NbtCompound().With("text", new NbtString("]")), new[] { "Steve" });
    var w3 = new MinecraftPrimitiveWriter();
    new TeamsPacket("reds", created772).Write(w3, 772);
    var nbtBytes = w3.ToArray();
    var r3 = new MinecraftPrimitiveReader(nbtBytes);
    var back3 = TeamsPacket.Read(ref r3, 772);
    var backNbt = back3.Action as TeamAction.CreatedV771_Last;
    Assert(backNbt is not null);
    Assert(((NbtCompound)backNbt!.Name).Items["text"] is NbtString { Value: "Reds" });
    Assert(backNbt.Flags == teamFlags);
    var w4 = new MinecraftPrimitiveWriter();
    back3.Write(w4, 772);
    Assert(Hex(w4.ToArray()) == Hex(nbtBytes));
    Console.WriteLine($"TeamsPacket @772: {nbtBytes.Length} bytes, re-write byte-identical, case={back3.Action.GetType().Name}");

    // the flags bits are one u8 either way, so modelling them moved no wire byte: clearing both
    // keeps the length and changes exactly the byte the old spec used to write as zero
    var w5 = new MinecraftPrimitiveWriter();
    new TeamsPacket("reds", created772 with { Flags = new TeamFlags(false, false) }).Write(w5, 772);
    var clearedBytes = w5.ToArray();
    Assert(clearedBytes.Length == nbtBytes.Length);
    Assert(clearedBytes.Zip(nbtBytes).Count(p => p.First != p.Second) == 1);
    Console.WriteLine($"TeamAction @772: flags travel as one u8 (friendly_fire, see_friendly_invisible)");

    // a case whose layer does not cover the version must fail, not invent a shape
    try
    {
        new TeamsPacket("reds", created).Write(new MinecraftPrimitiveWriter(), 772);
        Assert(false);
    }
    catch (NotSupportedException)
    {
    }

    var kind = back3.Action.Match(
        createdVUntil764: _ => "created", removed: _ => "removed", updatedVUntil764: _ => "updated",
        playersAdded: _ => "playersAdded", playersRemoved: _ => "playersRemoved",
        createdV771_Last: _ => "created", updatedV771_Last: _ => "updated",
        playersChanged: _ => "playersChanged");
    Assert(kind == "created");
    Console.WriteLine($"TeamAction.Match -> {kind}\n");
}

// --- UnionShapeProbe: the union shapes EntityMetadataValue is built from ---
// EntityMetadataValue itself cannot compile here yet (its arms read Slot, Particle and the
// registry variants, none of them modelled), so its three risky shapes are compiled and
// round-tripped through a probe union instead: keyword-named cases (Byte/Int/String), a case
// carrying a same-named type (Rotations), and a case carrying an array of one (Vec3f).
{
    var probes = new UnionShapeProbe[]
    {
        new UnionShapeProbe.Byte(-3),
        new UnionShapeProbe.Int(300),
        new UnionShapeProbe.String("hello"),
        new UnionShapeProbe.Rotations(new Rotations(1f, 2f, 3f)),
        new UnionShapeProbe.Vec3f(new[] { new Vec3f(1f, 2f, 3f), new Vec3f(-1f, 0f, 0.5f) }),
    };

    foreach (var probe in probes)
    {
        var w = new MinecraftPrimitiveWriter();
        probe.Write(w, version);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = UnionShapeProbe.Read(ref r, version, probe.Discriminator(version));
        Assert(back.GetType() == probe.GetType());

        // record equality compares array references, so the wire is the equality check
        var w2 = new MinecraftPrimitiveWriter();
        back.Write(w2, version);
        Assert(Hex(w2.ToArray()) == Hex(bytes));
    }

    var arrayWire = WireOf(probes[4]);
    var arrayReader = new MinecraftPrimitiveReader(arrayWire);
    var arrayBack = (UnionShapeProbe.Vec3f)UnionShapeProbe.Read(ref arrayReader, version, 4);
    Assert(arrayBack.Value.Length == 2 && arrayBack.Value[1] == new Vec3f(-1f, 0f, 0.5f));

    var kinds = probes.Select(p => p.Match(
        @byte: _ => "byte", @int: _ => "int", @string: _ => "string",
        @rotations: _ => "rotations", @vec3f: _ => "vec3f"));
    Assert(string.Join(",", kinds) == "byte,int,string,rotations,vec3f");
    Console.WriteLine($"UnionShapeProbe: {probes.Length} cases round-trip byte-identical, Match binds every case\n");

    static byte[] WireOf(UnionShapeProbe probe)
    {
        var w = new MinecraftPrimitiveWriter();
        probe.Write(w, MinecraftVersion.LatestProtocol);
        return w.ToArray();
    }
}

// --- GetPacketId: numeric ids from the McProtoFacts manifest ---
{
    Assert(SetProtocolPacket.GetPacketId(772) == 0x00);
    Assert(ServerInfoPacket.GetPacketId(772) == 0x00);
    Assert(PongResponsePacket.GetPacketId(772) == 0x01);
    Assert(PingStartPacket.GetPacketId(772) == 0x00);
    Assert(PingRequestPacket.GetPacketId(772) == 0x01);
    Console.WriteLine("GetPacketId: all asserted ids ok\n");
}

Console.WriteLine("done — poke away.");
