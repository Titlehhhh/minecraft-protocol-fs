using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;
using McProtoNet.NBT;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class RespawnPacket : IProtocolType<RespawnPacket>
{
    public string Dimension { get; }
    public NbtTag DimensionNbt { get; }
    public string DimensionName { get; }
    public string WorldName { get; }
    public long HashedSeed { get; }
    public int Gamemode { get; }
    public int PreviousGamemode { get; }
    public bool IsDebug { get; }
    public bool IsFlat { get; }
    public DeathLocation? Death { get; }
    public int PortalCooldown { get; }
    public bool CopyMetadata { get; }
    public SpawnInfo WorldState { get; }

    public RespawnPacket(string dimension, NbtTag dimensionNbt, string dimensionName, string worldName, long hashedSeed, int gamemode, int previousGamemode, bool isDebug, bool isFlat, DeathLocation? death, int portalCooldown, bool copyMetadata, SpawnInfo worldState)
    {
        Dimension = dimension;
        DimensionNbt = dimensionNbt;
        DimensionName = dimensionName;
        WorldName = worldName;
        HashedSeed = hashedSeed;
        Gamemode = gamemode;
        PreviousGamemode = previousGamemode;
        IsDebug = isDebug;
        IsFlat = isFlat;
        Death = death;
        PortalCooldown = portalCooldown;
        CopyMetadata = copyMetadata;
        WorldState = worldState;
    }

    public static RespawnPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<RespawnPacket>(protocolVersion);
        if (protocolVersion <= 736)
        {
            var dimension = reader.ReadString();
            var worldName = reader.ReadString();
            var hashedSeed = reader.ReadSignedLong();
            var gamemode = reader.ReadUnsignedByte();
            var previousGamemode = reader.ReadUnsignedByte();
            var isDebug = reader.ReadBoolean();
            var isFlat = reader.ReadBoolean();
            var copyMetadata = reader.ReadBoolean();
            return new RespawnPacket(dimension, default!, default!, worldName, hashedSeed, gamemode, previousGamemode, isDebug, isFlat, default!, default!, copyMetadata, default!);
        }

        if (protocolVersion >= 751 && protocolVersion <= 758)
        {
            var dimensionNbt = reader.ReadNbtTag(false);
            var worldName = reader.ReadString();
            var hashedSeed = reader.ReadSignedLong();
            var gamemode = reader.ReadUnsignedByte();
            var previousGamemode = reader.ReadUnsignedByte();
            var isDebug = reader.ReadBoolean();
            var isFlat = reader.ReadBoolean();
            var copyMetadata = reader.ReadBoolean();
            return new RespawnPacket(default!, dimensionNbt, default!, worldName, hashedSeed, gamemode, previousGamemode, isDebug, isFlat, default!, default!, copyMetadata, default!);
        }

        if (protocolVersion >= 759 && protocolVersion <= 759)
        {
            var dimensionName = reader.ReadString();
            var worldName = reader.ReadString();
            var hashedSeed = reader.ReadSignedLong();
            var gamemode = reader.ReadUnsignedByte();
            var previousGamemode = reader.ReadUnsignedByte();
            var isDebug = reader.ReadBoolean();
            var isFlat = reader.ReadBoolean();
            var copyMetadata = reader.ReadBoolean();
            DeathLocation? death = null;
            if (reader.ReadBoolean())
                death = reader.ReadType<DeathLocation>(protocolVersion);
            return new RespawnPacket(default!, default!, dimensionName, worldName, hashedSeed, gamemode, previousGamemode, isDebug, isFlat, death, default!, copyMetadata, default!);
        }

        if (protocolVersion >= 760 && protocolVersion <= 762)
        {
            var dimensionName = reader.ReadString();
            var worldName = reader.ReadString();
            var hashedSeed = reader.ReadSignedLong();
            var gamemode = reader.ReadSignedByte();
            var previousGamemode = reader.ReadUnsignedByte();
            var isDebug = reader.ReadBoolean();
            var isFlat = reader.ReadBoolean();
            var copyMetadata = reader.ReadBoolean();
            DeathLocation? death = null;
            if (reader.ReadBoolean())
                death = reader.ReadType<DeathLocation>(protocolVersion);
            return new RespawnPacket(default!, default!, dimensionName, worldName, hashedSeed, gamemode, previousGamemode, isDebug, isFlat, death, default!, copyMetadata, default!);
        }

        if (protocolVersion >= 763 && protocolVersion <= 763)
        {
            var dimensionName = reader.ReadString();
            var worldName = reader.ReadString();
            var hashedSeed = reader.ReadSignedLong();
            var gamemode = reader.ReadSignedByte();
            var previousGamemode = reader.ReadUnsignedByte();
            var isDebug = reader.ReadBoolean();
            var isFlat = reader.ReadBoolean();
            var copyMetadata = reader.ReadBoolean();
            DeathLocation? death = null;
            if (reader.ReadBoolean())
                death = reader.ReadType<DeathLocation>(protocolVersion);
            var portalCooldown = reader.ReadVarInt();
            return new RespawnPacket(default!, default!, dimensionName, worldName, hashedSeed, gamemode, previousGamemode, isDebug, isFlat, death, portalCooldown, copyMetadata, default!);
        }

        if (protocolVersion >= 764 && protocolVersion <= 765)
        {
            var dimensionName = reader.ReadString();
            var worldName = reader.ReadString();
            var hashedSeed = reader.ReadSignedLong();
            var gamemode = reader.ReadSignedByte();
            var previousGamemode = reader.ReadUnsignedByte();
            var isDebug = reader.ReadBoolean();
            var isFlat = reader.ReadBoolean();
            DeathLocation? death = null;
            if (reader.ReadBoolean())
                death = reader.ReadType<DeathLocation>(protocolVersion);
            var portalCooldown = reader.ReadVarInt();
            var copyMetadata = reader.ReadBoolean();
            return new RespawnPacket(default!, default!, dimensionName, worldName, hashedSeed, gamemode, previousGamemode, isDebug, isFlat, death, portalCooldown, copyMetadata, default!);
        }

        if (protocolVersion >= 766)
        {
            var worldState = reader.ReadType<SpawnInfo>(protocolVersion);
            var copyMetadata = reader.ReadUnsignedByte();
            return new RespawnPacket(default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, copyMetadata, worldState);
        }

        throw new System.NotSupportedException($"RespawnPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<RespawnPacket>(protocolVersion);
        if (protocolVersion <= 736)
        {
            writer.WriteString(Dimension);
            writer.WriteString(WorldName);
            writer.WriteSignedLong(HashedSeed);
            writer.WriteUnsignedByte((byte)Gamemode);
            writer.WriteUnsignedByte((byte)PreviousGamemode);
            writer.WriteBoolean(IsDebug);
            writer.WriteBoolean(IsFlat);
            writer.WriteBoolean(CopyMetadata);
            return;
        }

        if (protocolVersion >= 751 && protocolVersion <= 758)
        {
            writer.WriteNbt(DimensionNbt);
            writer.WriteString(WorldName);
            writer.WriteSignedLong(HashedSeed);
            writer.WriteUnsignedByte((byte)Gamemode);
            writer.WriteUnsignedByte((byte)PreviousGamemode);
            writer.WriteBoolean(IsDebug);
            writer.WriteBoolean(IsFlat);
            writer.WriteBoolean(CopyMetadata);
            return;
        }

        if (protocolVersion >= 759 && protocolVersion <= 759)
        {
            writer.WriteString(DimensionName);
            writer.WriteString(WorldName);
            writer.WriteSignedLong(HashedSeed);
            writer.WriteUnsignedByte((byte)Gamemode);
            writer.WriteUnsignedByte((byte)PreviousGamemode);
            writer.WriteBoolean(IsDebug);
            writer.WriteBoolean(IsFlat);
            writer.WriteBoolean(CopyMetadata);
            writer.WriteBoolean(Death is not null);
            if (Death is { } deathValue)
                writer.WriteType<DeathLocation>(deathValue, protocolVersion);
            return;
        }

        if (protocolVersion >= 760 && protocolVersion <= 762)
        {
            writer.WriteString(DimensionName);
            writer.WriteString(WorldName);
            writer.WriteSignedLong(HashedSeed);
            writer.WriteSignedByte((sbyte)Gamemode);
            writer.WriteUnsignedByte((byte)PreviousGamemode);
            writer.WriteBoolean(IsDebug);
            writer.WriteBoolean(IsFlat);
            writer.WriteBoolean(CopyMetadata);
            writer.WriteBoolean(Death is not null);
            if (Death is { } deathValue)
                writer.WriteType<DeathLocation>(deathValue, protocolVersion);
            return;
        }

        if (protocolVersion >= 763 && protocolVersion <= 763)
        {
            writer.WriteString(DimensionName);
            writer.WriteString(WorldName);
            writer.WriteSignedLong(HashedSeed);
            writer.WriteSignedByte((sbyte)Gamemode);
            writer.WriteUnsignedByte((byte)PreviousGamemode);
            writer.WriteBoolean(IsDebug);
            writer.WriteBoolean(IsFlat);
            writer.WriteBoolean(CopyMetadata);
            writer.WriteBoolean(Death is not null);
            if (Death is { } deathValue)
                writer.WriteType<DeathLocation>(deathValue, protocolVersion);
            writer.WriteVarInt(PortalCooldown);
            return;
        }

        if (protocolVersion >= 764 && protocolVersion <= 765)
        {
            writer.WriteString(DimensionName);
            writer.WriteString(WorldName);
            writer.WriteSignedLong(HashedSeed);
            writer.WriteSignedByte((sbyte)Gamemode);
            writer.WriteUnsignedByte((byte)PreviousGamemode);
            writer.WriteBoolean(IsDebug);
            writer.WriteBoolean(IsFlat);
            writer.WriteBoolean(Death is not null);
            if (Death is { } deathValue)
                writer.WriteType<DeathLocation>(deathValue, protocolVersion);
            writer.WriteVarInt(PortalCooldown);
            writer.WriteBoolean(CopyMetadata);
            return;
        }

        if (protocolVersion >= 766)
        {
            writer.WriteType<SpawnInfo>(WorldState, protocolVersion);
            writer.WriteUnsignedByte((byte)CopyMetadata);
            return;
        }

        throw new System.NotSupportedException($"RespawnPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
