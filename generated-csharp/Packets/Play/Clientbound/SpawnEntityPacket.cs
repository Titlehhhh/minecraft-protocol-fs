using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;
using System;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public readonly partial record struct SpawnEntityPacket(int EntityId, Guid ObjectUuid, int Type, double X, double Y, double Z, int Pitch, int Yaw, int HeadPitch, int ObjectData, int VelocityX, int VelocityY, int VelocityZ) : IProtocolType<SpawnEntityPacket>
{
    public static SpawnEntityPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<SpawnEntityPacket>(protocolVersion);
        if (protocolVersion <= 758)
        {
            var entityId = reader.ReadVarInt();
            var objectUuid = reader.ReadUUID();
            var type = reader.ReadVarInt();
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var pitch = reader.ReadSignedByte();
            var yaw = reader.ReadSignedByte();
            var objectData = reader.ReadSignedInt();
            var velocityX = reader.ReadSignedShort();
            var velocityY = reader.ReadSignedShort();
            var velocityZ = reader.ReadSignedShort();
            return new SpawnEntityPacket(entityId, objectUuid, type, x, y, z, pitch, yaw, default!, objectData, velocityX, velocityY, velocityZ);
        }

        if (protocolVersion >= 759)
        {
            var entityId = reader.ReadVarInt();
            var objectUuid = reader.ReadUUID();
            var type = reader.ReadVarInt();
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var pitch = reader.ReadSignedByte();
            var yaw = reader.ReadSignedByte();
            var headPitch = reader.ReadSignedByte();
            var objectData = reader.ReadVarInt();
            var velocityX = reader.ReadSignedShort();
            var velocityY = reader.ReadSignedShort();
            var velocityZ = reader.ReadSignedShort();
            return new SpawnEntityPacket(entityId, objectUuid, type, x, y, z, pitch, yaw, headPitch, objectData, velocityX, velocityY, velocityZ);
        }

        throw new System.NotSupportedException($"SpawnEntityPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<SpawnEntityPacket>(protocolVersion);
        if (protocolVersion <= 758)
        {
            writer.WriteVarInt(EntityId);
            writer.WriteUUID(ObjectUuid);
            writer.WriteVarInt(Type);
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteSignedByte((sbyte)Pitch);
            writer.WriteSignedByte((sbyte)Yaw);
            writer.WriteSignedInt(ObjectData);
            writer.WriteSignedShort((short)VelocityX);
            writer.WriteSignedShort((short)VelocityY);
            writer.WriteSignedShort((short)VelocityZ);
            return;
        }

        if (protocolVersion >= 759)
        {
            writer.WriteVarInt(EntityId);
            writer.WriteUUID(ObjectUuid);
            writer.WriteVarInt(Type);
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteSignedByte((sbyte)Pitch);
            writer.WriteSignedByte((sbyte)Yaw);
            writer.WriteSignedByte((sbyte)HeadPitch);
            writer.WriteVarInt(ObjectData);
            writer.WriteSignedShort((short)VelocityX);
            writer.WriteSignedShort((short)VelocityY);
            writer.WriteSignedShort((short)VelocityZ);
            return;
        }

        throw new System.NotSupportedException($"SpawnEntityPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
