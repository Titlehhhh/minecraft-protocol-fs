using McProtoNet.Protocol.Attributes;

namespace McProtoNet.Protocol.Packets.Play.Serverbound;

// Сейчас (сгенерировано):
//   record BlockDigPacket(int Status, Position Location, int Face, V759_LastLayer? V759_Last)
//   обработчик: packet.V759_Last?.Sequence ?? 0

// ---------- Вариант 1: union из C# 15 по целым формам ----------

public sealed record BlockDigV1(int Status, Position Location, int Face);
public sealed record BlockDigV2(int Status, Position Location, int Face, int Sequence);

[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
[Packet("play.toServer.block_dig", PacketPhase.Play, PacketDirection.Serverbound)]
public union BlockDigPacket1(
    [ProtocolSupport(MinecraftVersion.StartProtocol, 758)] BlockDigV1 Legacy,
    [ProtocolSupport(759, MinecraftVersion.LatestProtocol)] BlockDigV2 Sequenced);

static class Handler1
{
    static void Handle(BlockDigPacket1 packet)
    {
        // общие поля недоступны без ветвления
        var status = packet switch
        {
            BlockDigV1 p => p.Status,
            BlockDigV2 p => p.Status,
        };

        var seq = packet switch
        {
            BlockDigV1 => 0,
            BlockDigV2 p => p.Sequence,
        };
    }
}

// ---------- Вариант 2: closed-иерархия из C# 15, общее в базе ----------

[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
[Packet("play.toServer.block_dig", PacketPhase.Play, PacketDirection.Serverbound)]
public closed record BlockDigPacket2(int Status, Position Location, int Face)
{
    [ProtocolSupport(759, MinecraftVersion.LatestProtocol)]
    public sealed record Sequenced(int Status, Position Location, int Face, int Sequence)
        : BlockDigPacket2(Status, Location, Face);
}

static class Handler2
{
    static void Handle(BlockDigPacket2 packet)
    {
        var status = packet.Status;                       // общее поле всегда есть

        if (packet is BlockDigPacket2.Sequenced s)        // добавка только там, где она есть
            Ack(s.Sequence);

        var seq = packet switch                           // исчерпывающе, без default
        {
            BlockDigPacket2.Sequenced p => p.Sequence,
            BlockDigPacket2 => 0,
        };
    }

    static void Ack(int sequence) { }
}

// ---------- Вариант 3: плоское nullable, без новых фич ----------

[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
[Packet("play.toServer.block_dig", PacketPhase.Play, PacketDirection.Serverbound)]
public sealed record BlockDigPacket3(
    int Status,
    Position Location,
    int Face,
    [property: ProtocolSupport(759, MinecraftVersion.LatestProtocol)] int? Sequence);

static class Handler3
{
    static void Handle(BlockDigPacket3 packet)
    {
        var status = packet.Status;

        if (packet.Sequence is { } seq)
            Ack(seq);
    }

    static void Ack(int sequence) { }
}
