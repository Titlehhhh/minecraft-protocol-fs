using McProtoNet.Protocol;
using McProtoNet.Protocol.Packets.Handshaking.Serverbound;
using McProtoNet.Protocol.Packets.Login.Clientbound;
using McProtoNet.Protocol.Packets.Login.Serverbound;
using McProtoNet.Protocol.Packets.Play.Clientbound;
using McProtoNet.Protocol.Packets.Status.Clientbound;
using McProtoNet.Protocol.Packets.Status.Serverbound;
using McProtoNet.Primitives;
using UseEntityPacket = McProtoNet.Protocol.Packets.Play.Serverbound.UseEntityPacket;
using ChatMessagePacket = McProtoNet.Protocol.Packets.Play.Serverbound.ChatMessagePacket;
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
    var info = new SpawnInfo(2, "minecraft:overworld", 12345L, Gamemode.Creative, 0, false, true, death, 3, 63);
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

// --- UseEntityPacket: union era @774 (interact_at arm), flat era @776 (LpVec3) ---
{
    var w764 = new MinecraftPrimitiveWriter();
    var atPacket = new UseEntityPacket(42, true, VUntil774: new(new InteractAction.InteractAt(0.5f, 1.25f, -0.75f, 1)));
    atPacket.Write(w764, 774);
    var bytes764 = w764.ToArray();
    var r764 = new MinecraftPrimitiveReader(bytes764);
    var back764 = UseEntityPacket.Read(ref r764, 774);
    var at = back764.VUntil774!.Value.Action as InteractAction.InteractAt;
    Assert(back764.Target == 42 && back764.Sneaking);
    Assert(at is not null && at.X == 0.5f && at.Y == 1.25f && at.Z == -0.75f && at.Hand == 1);
    Assert(bytes764[1] == 2);

    var again = new MinecraftPrimitiveWriter();
    back764.Write(again, 774);
    Assert(Hex(again.ToArray()) == Hex(bytes764));

    var w776 = new MinecraftPrimitiveWriter();
    var flat = new UseEntityPacket(42, false, V775_Last: new(0, new LpVec3(0.25d, -0.5d, 1.0d)));
    flat.Write(w776, 776);
    var bytes776 = w776.ToArray();
    var r776 = new MinecraftPrimitiveReader(bytes776);
    var back776 = UseEntityPacket.Read(ref r776, 776);
    var loc = back776.V775_Last!.Value.Location;
    Assert(back776.VUntil774 is null && Math.Abs(loc.Z - 1.0d) <= 2d / 32766d);

    Console.WriteLine($"UseEntityPacket @774: {bytes764.Length} bytes, discriminator={bytes764[1]}, case={at!.GetType().Name}");
    Console.WriteLine($"UseEntityPacket @776: {bytes776.Length} bytes, location={loc}");
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

// --- FixedBytes: exactly-n bytes, no length prefix ---
{
    var packet = new McProtoNet.Protocol.Packets.Play.Serverbound.ChatCommandSignedPacket(
        "seed", 1234L, 5678L,
        new[] { new ArgumentSignature("target", Enumerable.Range(0, 256).Select(i => (byte)i).ToArray()) },
        3, new byte[] { 1, 2, 3 },
        new McProtoNet.Protocol.Packets.Play.Serverbound.ChatCommandSignedPacket.V770_LastLayer(7));

    var w = new MinecraftPrimitiveWriter();
    packet.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = McProtoNet.Protocol.Packets.Play.Serverbound.ChatCommandSignedPacket.Read(ref r, version);

    Assert(back.Acknowledged.Length == 3 && Hex(back.Acknowledged) == "010203");
    Assert(back.ArgumentSignatures[0].Signature.Length == 256);
    Assert(Hex(back.ArgumentSignatures[0].Signature) == Hex(packet.ArgumentSignatures[0].Signature));
    Assert(back.V770_Last is { Checksum: 7 });

    var w2 = new MinecraftPrimitiveWriter();
    back.Write(w2, version);
    Assert(Hex(w2.ToArray()) == Hex(bytes));

    var wrongLength = packet with { Acknowledged = new byte[] { 1, 2 } };
    var threw = false;
    try
    {
        wrongLength.Write(new MinecraftPrimitiveWriter(), version);
    }
    catch (ArgumentException)
    {
        threw = true;
    }

    Assert(threw);
    Console.WriteLine($"ChatCommandSignedPacket: FixedBytes round-trips, wrong length rejected ({bytes.Length} bytes)");
    Console.WriteLine();
}

// --- Conditional groups: readOpt / ifNonZero + readBlock ---
{
    foreach (var probe in new[]
             {
                 new ConditionalShapeProbe(0, new byte[] { 9, 8, 7, 6 }, 1, new Rotations(1f, 2f, 3f)),
                 new ConditionalShapeProbe(0, new byte[] { 0, 0, 0, 0 }, 0, null),
                 new ConditionalShapeProbe(5, null, 2, new Rotations(-1f, 0f, 0.5f)),
                 new ConditionalShapeProbe(5, null, 0, null),
             })
    {
        var w = new MinecraftPrimitiveWriter();
        probe.Write(w, version);
        var bytes = w.ToArray();
        var r = new MinecraftPrimitiveReader(bytes);
        var back = ConditionalShapeProbe.Read(ref r, version);

        Assert(back.Kind == probe.Kind && back.Flag == probe.Flag);
        Assert((back.Signature is null) == (probe.Signature is null));
        Assert(back.Signature is null || Hex(back.Signature) == Hex(probe.Signature!));
        Assert(back.Block.Equals(probe.Block));

        var w2 = new MinecraftPrimitiveWriter();
        back.Write(w2, version);
        Assert(Hex(w2.ToArray()) == Hex(bytes));
    }

    // the guard must test the byte that actually travels: Flag = 256 writes 0 over the u8 wire,
    // so the reader will not look for the block and the writer must not emit one
    {
        var wide = new ConditionalShapeProbe(5, null, 256, new Rotations(1f, 2f, 3f));
        var w = new MinecraftPrimitiveWriter();
        wide.Write(w, version);
        var bytes = w.ToArray();
        Assert(bytes.Length == 2);

        var r = new MinecraftPrimitiveReader(bytes);
        var back = ConditionalShapeProbe.Read(ref r, version);
        Assert(back.Flag == 0 && back.Block is null);
    }

    // a value the discriminator does not select has nowhere to go on the wire
    var orphan = new ConditionalShapeProbe(5, new byte[4], 0, null);
    var orphanThrew = false;
    try
    {
        orphan.Write(new MinecraftPrimitiveWriter(), version);
    }
    catch (InvalidOperationException)
    {
        orphanThrew = true;
    }

    Assert(orphanThrew);

    // an absent value the wire demands must fail loudly, not write a hole
    var missing = new ConditionalShapeProbe(0, null, 1, new Rotations(0f, 0f, 0f));
    var threw = false;
    try
    {
        missing.Write(new MinecraftPrimitiveWriter(), version);
    }
    catch (InvalidOperationException)
    {
        threw = true;
    }

    Assert(threw);
    Console.WriteLine("ConditionalShapeProbe: 4 shapes round-trip byte-identical, missing required value throws");
    Console.WriteLine();
}

// --- NBT: the container tags (byte/int/long arrays, lists), nested and byte-identical ---
{
    static byte[] WriteTag(NbtTag tag)
    {
        var w = new MinecraftPrimitiveWriter();
        w.WriteNbt(tag);
        return w.ToArray();
    }

    static NbtTag ReadTag(byte[] bytes)
    {
        var r = new MinecraftPrimitiveReader(bytes);
        return r.ReadNbtTag(false)!;
    }

    var root = new NbtCompound()
        .With("bytes", new NbtByteArray(new byte[] { 0, 1, 0x7F, 0x80, 0xFF }))
        .With("ints", new NbtIntArray(new[] { 0, -1, int.MinValue, int.MaxValue }))
        .With("longs", new NbtLongArray(new[] { 0L, -1L, long.MinValue, long.MaxValue }))
        .With("empty", new NbtList(0, Array.Empty<NbtTag>()))
        .With("strings", NbtList.Of(new NbtString("a"), new NbtString("bb")))
        .With("compounds", NbtList.Of(
            new NbtCompound().With("id", new NbtInt(1)),
            new NbtCompound().With("id", new NbtInt(2))))
        .With("listOfLists", NbtList.Of(
            NbtList.Of(new NbtByte(1), new NbtByte(2)),
            NbtList.Of(new NbtByte(3))))
        .With("nested", new NbtCompound().With("deep", new NbtLongArray(new[] { 42L })));

    var nbtBytes = WriteTag(root);
    var readBack = (NbtCompound)ReadTag(nbtBytes);
    Assert(Hex(WriteTag(readBack)) == Hex(nbtBytes));

    Assert(((NbtByteArray)readBack.Items["bytes"]).Value.SequenceEqual(new byte[] { 0, 1, 0x7F, 0x80, 0xFF }));
    Assert(((NbtIntArray)readBack.Items["ints"]).Value.SequenceEqual(new[] { 0, -1, int.MinValue, int.MaxValue }));
    Assert(((NbtLongArray)readBack.Items["longs"]).Value.SequenceEqual(new[] { 0L, -1L, long.MinValue, long.MaxValue }));
    Assert(((NbtList)readBack.Items["empty"]).Items.Count == 0);
    Assert(((NbtList)readBack.Items["strings"]).ElementId == 8);
    Assert(((NbtString)((NbtList)readBack.Items["strings"]).Items[1]).Value == "bb");
    Assert(((NbtList)readBack.Items["compounds"]).ElementId == 10);
    Assert(((NbtInt)((NbtCompound)((NbtList)readBack.Items["compounds"]).Items[1]).Items["id"]).Value == 2);

    var outer = (NbtList)readBack.Items["listOfLists"];
    Assert(outer.ElementId == 9 && outer.Items.Count == 2);
    Assert(((NbtList)outer.Items[0]).Items.Count == 2 && ((NbtList)outer.Items[1]).Items.Count == 1);
    Assert(((NbtByte)((NbtList)outer.Items[1]).Items[0]).Value == 3);

    // arrays and list counts are a big-endian signed int, not a varint
    var lenBytes = WriteTag(new NbtIntArray(new[] { 7 }));
    Assert(Hex(lenBytes) == "0B" + "00000001" + "00000007");

    // a list whose declared element id disagrees with what it holds would shift the wire
    var mismatchThrew = false;
    try
    {
        WriteTag(new NbtList(1, new NbtTag[] { new NbtString("a") }));
    }
    catch (NotSupportedException)
    {
        mismatchThrew = true;
    }

    Assert(mismatchThrew);

    // an unmodelled tag id still throws instead of guessing a payload length
    var unknownThrew = false;
    try
    {
        ReadTag(new byte[] { 13, 0, 0, 0, 0 });
    }
    catch (NotSupportedException)
    {
        unknownThrew = true;
    }

    Assert(unknownThrew);
    Console.WriteLine("NBT: byte/int/long arrays and nested lists round-trip byte-identical, unknown tag id throws\n");
}

// --- RegistryOrInline<T>: varint 0 = inline payload, n > 0 = registry entry n - 1 ---
{
    var inline = RegistryOrInline<Position>.Inline(new Position(1, 2, 3));
    var wi = new MinecraftPrimitiveWriter();
    inline.Write(wi, version);
    var inlineBytes = wi.ToArray();
    Assert(inlineBytes.Length == 9 && inlineBytes[0] == 0);

    var ri = new MinecraftPrimitiveReader(inlineBytes);
    var backInline = RegistryOrInline<Position>.Read(ref ri, version);
    Assert(backInline.IsInline && backInline.Value == new Position(1, 2, 3));

    foreach (var id in new[] { 0, 1, 127, 128 })
    {
        var wr = new MinecraftPrimitiveWriter();
        RegistryOrInline<Position>.FromRegistry(id).Write(wr, version);
        var registryBytes = wr.ToArray();

        var rr = new MinecraftPrimitiveReader(registryBytes);
        var backRegistry = RegistryOrInline<Position>.Read(ref rr, version);
        Assert(backRegistry.IsRegistry && backRegistry.Id == id);
        Assert(rr.Position == registryBytes.Length);
    }

    // registry id 0 travels as 1, so the inline arm keeps 0 to itself
    var w0 = new MinecraftPrimitiveWriter();
    RegistryOrInline<Position>.FromRegistry(0).Write(w0, version);
    Assert(Hex(w0.ToArray()) == "01");

    Console.WriteLine("RegistryOrInline<Position>: both arms round-trip, registry ids offset by one\n");
}

// --- HolderShapeProbe: generated code reading and writing RegistryHolder fields ---
{
    var probe = new HolderShapeProbe(
        7,
        RegistryOrInline<ItemSoundEvent>.FromRegistry(42),
        new[]
        {
            RegistryOrInline<ItemSoundEvent>.Inline(new ItemSoundEvent("minecraft:entity.pig.ambient", null)),
            RegistryOrInline<ItemSoundEvent>.FromRegistry(0),
            RegistryOrInline<ItemSoundEvent>.Inline(new ItemSoundEvent("minecraft:block.stone.break", 16f)),
        },
        9);

    var w = new MinecraftPrimitiveWriter();
    probe.Write(w, version);
    var bytes = w.ToArray();
    var r = new MinecraftPrimitiveReader(bytes);
    var back = HolderShapeProbe.Read(ref r, version);

    Assert(r.Position == bytes.Length);
    Assert(back.Before == 7 && back.After == 9);
    Assert(back.Sound.IsRegistry && back.Sound.Id == 42);
    Assert(back.Sounds.Length == 3);
    Assert(back.Sounds[0].IsInline && back.Sounds[0].Value.SoundName == "minecraft:entity.pig.ambient");
    Assert(back.Sounds[0].Value.FixedRange is null);
    Assert(back.Sounds[1].IsRegistry && back.Sounds[1].Id == 0);
    Assert(back.Sounds[2].IsInline && back.Sounds[2].Value.FixedRange == 16f);

    var w2 = new MinecraftPrimitiveWriter();
    back.Write(w2, version);
    Assert(Hex(w2.ToArray()) == Hex(bytes));

    // the holder is one varint plus, for the inline arm only, the payload — no wrapper object
    var wOne = new MinecraftPrimitiveWriter();
    wOne.WriteType(RegistryOrInline<ItemSoundEvent>.FromRegistry(0), version);
    Assert(Hex(wOne.ToArray()) == "01");

    // 761 is the first version that has the payload type; 760 must refuse, not invent a shape
    var tooOldThrew = false;
    try
    {
        var rOld = new MinecraftPrimitiveReader(bytes);
        HolderShapeProbe.Read(ref rOld, 760);
    }
    catch (InvalidOperationException)
    {
        tooOldThrew = true;
    }

    Assert(tooOldThrew);
    Console.WriteLine($"HolderShapeProbe: {bytes.Length} bytes, both holder arms and an array of them re-write byte-identical\n");
}

// --- EnumShapeProbe: named values, unknown ids, per-site backings, per-version tables ---
{
    static byte[] Bytes(EnumShapeProbe p, int v)
    {
        var w = new MinecraftPrimitiveWriter();
        p.Write(w, v);
        return w.ToArray();
    }

    foreach (var v in new[] { 770, 772 })
    {
        var probe = new EnumShapeProbe(7, SoundSource.Player, Gamemode.Adventure, Difficulty.Hard, 9);
        var bytes = Bytes(probe, v);
        var r = new MinecraftPrimitiveReader(bytes);
        var back = EnumShapeProbe.Read(ref r, v);

        Assert(r.Position == bytes.Length);
        Assert(back.Sound == SoundSource.Player && back.Mode == Gamemode.Adventure);
        Assert(back.Diff == Difficulty.Hard && back.Before == 7 && back.After == 9);
        Assert(Hex(Bytes(back, v)) == Hex(bytes));

        // an id no table names survives the round-trip as its raw value, it never throws
        var unknown = new EnumShapeProbe(0, new SoundSource(99), new Gamemode(120), new Difficulty(7), 0);
        var uBytes = Bytes(unknown, v);
        var ur = new MinecraftPrimitiveReader(uBytes);
        var uBack = EnumShapeProbe.Read(ref ur, v);

        Assert(ur.Position == uBytes.Length);
        Assert(uBack.Sound.Value == 99 && uBack.Mode.Value == 120 && uBack.Diff.Value == 7);
        Assert(uBack.Sound.ToString() == "unknown(99)");
        Assert(Hex(Bytes(uBack, v)) == Hex(uBytes));

        Console.WriteLine($"EnumShapeProbe @{v}: {bytes.Length} bytes, named + unknown ids re-write byte-identical");
    }

    // the same table under two backings: gamemode is i8 until 770 and varint from 771, and
    // difficulty is u8 until 770 and varint from 771 — both one byte for these ids, so the
    // layouts must be told apart by which reader call they made, not by length
    Assert(Bytes(new EnumShapeProbe(0, SoundSource.Master, Gamemode.Spectator, Difficulty.Peaceful, 0), 770).Length
           == Bytes(new EnumShapeProbe(0, SoundSource.Master, Gamemode.Spectator, Difficulty.Peaceful, 0), 772).Length);

    // multi-layout table: `ui` exists from 771 only, and the merged ToString() knows every name
    Assert(SoundSource.Ui.ToString(770) == "unknown(10)");
    Assert(SoundSource.Ui.ToString(772) == "ui");
    Assert(SoundSource.Ui.ToString() == "ui");
    Assert(SoundSource.Voice.ToString(770) == "voice");
    Assert(((int)Difficulty.Normal) == 2 && ((Difficulty)2) == Difficulty.Normal);

    // outside every layout the gate refuses rather than inventing a shape
    var beforeSupport = false;
    try
    {
        var r765 = new MinecraftPrimitiveReader(new byte[] { 0, 0, 0, 0, 0 });
        EnumShapeProbe.Read(ref r765, 765);
    }
    catch (InvalidOperationException)
    {
        beforeSupport = true;
    }

    Assert(beforeSupport);

    var soundTooOld = false;
    try
    {
        var rOld = new MinecraftPrimitiveReader(new byte[] { 0 });
        SoundSource.Read(ref rOld, 760);
    }
    catch (InvalidOperationException)
    {
        soundTooOld = true;
    }

    Assert(soundTooOld);
    Console.WriteLine("EnumShapeProbe: per-version tables, explicit conversions and the version gate all hold\n");
}

// --- PlayerChatPacket / ChatMessagePacket: the signed-chat chain across its version layers ---
{
    static byte[] WritePlayerChat(PlayerChatPacket p, int v)
    {
        var w = new MinecraftPrimitiveWriter();
        p.Write(w, v);
        return w.ToArray();
    }

    static byte[] WriteChatMessage(ChatMessagePacket p, int v)
    {
        var w = new MinecraftPrimitiveWriter();
        p.Write(w, v);
        return w.ToArray();
    }

    var sender = Guid.Parse("f84c6a79-0a4e-45e0-879b-cd49ebd4c4e2");
    var signature = new byte[256];
    for (int i = 0; i < signature.Length; i++) signature[i] = (byte)(i * 7);

    var previous = new[]
    {
        new PreviousMessage(default, default!, 0, signature),
        new PreviousMessage(default, default!, 3, null),
    };

    // 772: globalIndex, a holder-carried chat type and NBT chat components
    {
        var packet = new PlayerChatPacket(
            sender, signature, 1_700_000_000_000L, -4242L,
            V770_Last: new PlayerChatPacket.V770_LastLayer(
                17, 4, "hello world", previous,
                new NbtCompound().With("text", new NbtString("hello world")),
                2, new[] { 1L, 2L, 3L },
                RegistryOrInline<ChatTypes>.FromRegistry(1),
                new NbtCompound().With("text", new NbtString("Steve")),
                null));

        var bytes = WritePlayerChat(packet, 772);
        var r = new MinecraftPrimitiveReader(bytes);
        var back = PlayerChatPacket.Read(ref r, 772);

        Assert(r.Position == bytes.Length);
        Assert(back.SenderUuid == sender && back.Timestamp == 1_700_000_000_000L && back.Salt == -4242L);
        Assert(back.V770_Last is not null && back.V759 is null && back.V767_769 is null);

        var layer = back.V770_Last!.Value;
        Assert(layer.GlobalIndex == 17 && layer.Index == 4 && layer.PlainMessage == "hello world");
        Assert(layer.PreviousMessages.Length == 2);
        Assert(layer.PreviousMessages[0].Id == 0 && Hex(layer.PreviousMessages[0].Signature!) == Hex(signature));
        Assert(layer.PreviousMessages[1].Id == 3 && layer.PreviousMessages[1].Signature is null);
        Assert(layer.FilterType == 2 && layer.FilterTypeMask is { Length: 3 });
        Assert(layer.ChatType.IsRegistry && layer.ChatType.Id == 1);
        Assert(((NbtCompound)layer.NetworkName).Items["text"] is NbtString { Value: "Steve" });
        Assert(layer.NetworkTargetName is null);
        Assert(Hex(WritePlayerChat(back, 772)) == Hex(bytes));

        Console.WriteLine($"PlayerChatPacket @772: {bytes.Length} bytes, re-writes byte-identical");
    }

    // 767: the same holder, inline this time — the chat type travels as a payload, not an id
    {
        var chatType = new ChatTypes(
            new ChatType("chat.type.text", new[] { ChatTypeParameterType.Sender, ChatTypeParameterType.Content },
                new NbtCompound().With("color", new NbtString("white"))),
            new ChatType("chat.type.text.narrate", new[] { ChatTypeParameterType.Content },
                new NbtCompound()));

        var packet = new PlayerChatPacket(
            sender, null, 1L, 2L,
            V767_769: new PlayerChatPacket.V767_769Layer(
                0, "inline", Array.Empty<PreviousMessage>(), null, 0, null,
                RegistryOrInline<ChatTypes>.Inline(chatType),
                new NbtCompound().With("text", new NbtString("Alex")),
                new NbtCompound().With("text", new NbtString("Steve"))));

        var bytes = WritePlayerChat(packet, 767);
        var r = new MinecraftPrimitiveReader(bytes);
        var back = PlayerChatPacket.Read(ref r, 767);

        Assert(r.Position == bytes.Length);
        Assert(back.Signature is null);

        var layer = back.V767_769!.Value;
        Assert(layer.ChatType.IsInline);
        Assert(layer.ChatType.Value.Chat.TranslationKey == "chat.type.text");
        Assert(layer.ChatType.Value.Chat.Parameters.Length == 2);
        Assert(layer.ChatType.Value.Chat.Parameters[0] == ChatTypeParameterType.Sender);
        Assert(layer.ChatType.Value.Narration.Parameters.Length == 1);
        Assert(layer.FilterType == 0 && layer.FilterTypeMask is null);
        Assert(Hex(WritePlayerChat(back, 767)) == Hex(bytes));

        Console.WriteLine($"PlayerChatPacket @767: {bytes.Length} bytes, inline holder round-trips");
    }

    // 761: the oldest layer that still has today's shape — json components, no global index
    {
        var packet = new PlayerChatPacket(
            sender, signature, 5L, 6L,
            V761_764: new PlayerChatPacket.V761_764Layer(
                1, "legacy", previous, "{\"text\":\"legacy\"}", 2, new[] { -1L },
                7, "{\"text\":\"Alex\"}", null));

        var bytes = WritePlayerChat(packet, 761);
        var r = new MinecraftPrimitiveReader(bytes);
        var back = PlayerChatPacket.Read(ref r, 761);

        Assert(r.Position == bytes.Length);
        Assert(back.V761_764 is not null && back.V770_Last is null);
        Assert(back.V761_764!.Value.Type == 7 && back.V761_764!.Value.NetworkNameJson == "{\"text\":\"Alex\"}");
        Assert(Hex(WritePlayerChat(back, 761)) == Hex(bytes));

        Console.WriteLine($"PlayerChatPacket @761: {bytes.Length} bytes, re-writes byte-identical");
    }

    // serverbound: 772 carries the checksum byte, 761-769 does not
    {
        var acknowledged = new byte[] { 0x01, 0x00, 0x80 };

        var packet = new ChatMessagePacket(
            "/say hi", 1_700_000_000_000L, 99L, signature,
            V770_Last: new ChatMessagePacket.V770_LastLayer(12, acknowledged, 0xAB));

        var bytes = WriteChatMessage(packet, 772);
        var r = new MinecraftPrimitiveReader(bytes);
        var back = ChatMessagePacket.Read(ref r, 772);

        Assert(r.Position == bytes.Length);
        Assert(back.Message == "/say hi" && back.Salt == 99L);
        Assert(Hex(back.Signature!) == Hex(signature));
        Assert(back.V770_Last!.Value.Offset == 12 && back.V770_Last!.Value.Checksum == 0xAB);
        Assert(Hex(back.V770_Last!.Value.Acknowledged) == Hex(acknowledged));
        Assert(Hex(WriteChatMessage(back, 772)) == Hex(bytes));

        var older = new ChatMessagePacket(
            "unsigned", 1L, 2L, null,
            V761_769: new ChatMessagePacket.V761_769Layer(0, acknowledged));

        var oldBytes = WriteChatMessage(older, 769);
        var ro = new MinecraftPrimitiveReader(oldBytes);
        var oldBack = ChatMessagePacket.Read(ref ro, 769);

        Assert(ro.Position == oldBytes.Length);
        Assert(oldBack.Signature is null && oldBack.V761_769 is not null && oldBack.V770_Last is null);
        Assert(Hex(WriteChatMessage(oldBack, 769)) == Hex(oldBytes));

        Console.WriteLine($"ChatMessagePacket: {bytes.Length} bytes @772, {oldBytes.Length} bytes @769, both re-write byte-identical\n");
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
