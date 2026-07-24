using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Clientbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class PlayerPositionPacket : IProtocolType<PlayerPositionPacket>
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public float Yaw { get; }
    public float Pitch { get; }
    public PositionUpdateRelatives Flags { get; }
    public int TeleportId { get; }
    public bool DismountVehicle { get; }
    public double Dx { get; }
    public double Dy { get; }
    public double Dz { get; }

    public PlayerPositionPacket(double x, double y, double z, float yaw, float pitch, PositionUpdateRelatives flags, int teleportId, bool dismountVehicle, double dx, double dy, double dz)
    {
        X = x;
        Y = y;
        Z = z;
        Yaw = yaw;
        Pitch = pitch;
        Flags = flags;
        TeleportId = teleportId;
        DismountVehicle = dismountVehicle;
        Dx = dx;
        Dy = dy;
        Dz = dz;
    }

    public static PlayerPositionPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<PlayerPositionPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var yaw = reader.ReadFloat();
            var pitch = reader.ReadFloat();
            var flags = reader.ReadType<PositionUpdateRelatives>(protocolVersion);
            var teleportId = reader.ReadVarInt();
            return new PlayerPositionPacket(x, y, z, yaw, pitch, flags, teleportId, default!, default!, default!, default!);
        }

        if (protocolVersion >= 755 && protocolVersion <= 761)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var yaw = reader.ReadFloat();
            var pitch = reader.ReadFloat();
            var flags = reader.ReadType<PositionUpdateRelatives>(protocolVersion);
            var teleportId = reader.ReadVarInt();
            var dismountVehicle = reader.ReadBoolean();
            return new PlayerPositionPacket(x, y, z, yaw, pitch, flags, teleportId, dismountVehicle, default!, default!, default!);
        }

        if (protocolVersion >= 762 && protocolVersion <= 765)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var yaw = reader.ReadFloat();
            var pitch = reader.ReadFloat();
            var flags = reader.ReadType<PositionUpdateRelatives>(protocolVersion);
            var teleportId = reader.ReadVarInt();
            return new PlayerPositionPacket(x, y, z, yaw, pitch, flags, teleportId, default!, default!, default!, default!);
        }

        if (protocolVersion >= 766 && protocolVersion <= 767)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var yaw = reader.ReadFloat();
            var pitch = reader.ReadFloat();
            var flags = reader.ReadType<PositionUpdateRelatives>(protocolVersion);
            var teleportId = reader.ReadVarInt();
            return new PlayerPositionPacket(x, y, z, yaw, pitch, flags, teleportId, default!, default!, default!, default!);
        }

        if (protocolVersion >= 768)
        {
            var teleportId = reader.ReadVarInt();
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var dx = reader.ReadDouble();
            var dy = reader.ReadDouble();
            var dz = reader.ReadDouble();
            var yaw = reader.ReadFloat();
            var pitch = reader.ReadFloat();
            var flags = reader.ReadType<PositionUpdateRelatives>(protocolVersion);
            return new PlayerPositionPacket(x, y, z, yaw, pitch, flags, teleportId, default!, dx, dy, dz);
        }

        throw new System.NotSupportedException($"PlayerPositionPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<PlayerPositionPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteFloat(Yaw);
            writer.WriteFloat(Pitch);
            writer.WriteType<PositionUpdateRelatives>(Flags, protocolVersion);
            writer.WriteVarInt(TeleportId);
            return;
        }

        if (protocolVersion >= 755 && protocolVersion <= 761)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteFloat(Yaw);
            writer.WriteFloat(Pitch);
            writer.WriteType<PositionUpdateRelatives>(Flags, protocolVersion);
            writer.WriteVarInt(TeleportId);
            writer.WriteBoolean(DismountVehicle);
            return;
        }

        if (protocolVersion >= 762 && protocolVersion <= 765)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteFloat(Yaw);
            writer.WriteFloat(Pitch);
            writer.WriteType<PositionUpdateRelatives>(Flags, protocolVersion);
            writer.WriteVarInt(TeleportId);
            return;
        }

        if (protocolVersion >= 766 && protocolVersion <= 767)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteFloat(Yaw);
            writer.WriteFloat(Pitch);
            writer.WriteType<PositionUpdateRelatives>(Flags, protocolVersion);
            writer.WriteVarInt(TeleportId);
            return;
        }

        if (protocolVersion >= 768)
        {
            writer.WriteVarInt(TeleportId);
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteDouble(Dx);
            writer.WriteDouble(Dy);
            writer.WriteDouble(Dz);
            writer.WriteFloat(Yaw);
            writer.WriteFloat(Pitch);
            writer.WriteType<PositionUpdateRelatives>(Flags, protocolVersion);
            return;
        }

        throw new System.NotSupportedException($"PlayerPositionPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
