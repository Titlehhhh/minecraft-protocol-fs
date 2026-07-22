using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class ExplosionPacket : IProtocolType<ExplosionPacket>
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public float Radius { get; }
    public ExplosionBlockOffset[] AffectedBlockOffsets { get; }
    public float PlayerMotionX { get; }
    public float PlayerMotionY { get; }
    public float PlayerMotionZ { get; }
    public int BlockInteractionType { get; }
    public Particle SmallExplosionParticle { get; }
    public Particle LargeExplosionParticle { get; }
    public ItemSoundHolder Sound { get; }
    public Vec3f64? PlayerKnockback { get; }
    public Particle ExplosionParticle { get; }

    public ExplosionPacket(double x, double y, double z, float radius, ExplosionBlockOffset[] affectedBlockOffsets, float playerMotionX, float playerMotionY, float playerMotionZ, int blockInteractionType, Particle smallExplosionParticle, Particle largeExplosionParticle, ItemSoundHolder sound, Vec3f64? playerKnockback, Particle explosionParticle)
    {
        X = x;
        Y = y;
        Z = z;
        Radius = radius;
        AffectedBlockOffsets = affectedBlockOffsets;
        PlayerMotionX = playerMotionX;
        PlayerMotionY = playerMotionY;
        PlayerMotionZ = playerMotionZ;
        BlockInteractionType = blockInteractionType;
        SmallExplosionParticle = smallExplosionParticle;
        LargeExplosionParticle = largeExplosionParticle;
        Sound = sound;
        PlayerKnockback = playerKnockback;
        ExplosionParticle = explosionParticle;
    }

    public static ExplosionPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<ExplosionPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            var x = reader.ReadFloat();
            var y = reader.ReadFloat();
            var z = reader.ReadFloat();
            var radius = reader.ReadFloat();
            int affectedBlockOffsetsCount = checked((int)reader.ReadSignedInt());
            var affectedBlockOffsets = new ExplosionBlockOffset[affectedBlockOffsetsCount];
            for (int i = 0; i < affectedBlockOffsets.Length; i++)
                affectedBlockOffsets[i] = reader.ReadType<ExplosionBlockOffset>(protocolVersion);
            var playerMotionX = reader.ReadFloat();
            var playerMotionY = reader.ReadFloat();
            var playerMotionZ = reader.ReadFloat();
            return new ExplosionPacket(x, y, z, radius, affectedBlockOffsets, playerMotionX, playerMotionY, playerMotionZ, default!, default!, default!, default!, default!, default!);
        }

        if (protocolVersion >= 755 && protocolVersion <= 760)
        {
            var x = reader.ReadFloat();
            var y = reader.ReadFloat();
            var z = reader.ReadFloat();
            var radius = reader.ReadFloat();
            int affectedBlockOffsetsCount = reader.ReadVarInt();
            var affectedBlockOffsets = new ExplosionBlockOffset[affectedBlockOffsetsCount];
            for (int i = 0; i < affectedBlockOffsets.Length; i++)
                affectedBlockOffsets[i] = reader.ReadType<ExplosionBlockOffset>(protocolVersion);
            var playerMotionX = reader.ReadFloat();
            var playerMotionY = reader.ReadFloat();
            var playerMotionZ = reader.ReadFloat();
            return new ExplosionPacket(x, y, z, radius, affectedBlockOffsets, playerMotionX, playerMotionY, playerMotionZ, default!, default!, default!, default!, default!, default!);
        }

        if (protocolVersion >= 761 && protocolVersion <= 764)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var radius = reader.ReadFloat();
            int affectedBlockOffsetsCount = reader.ReadVarInt();
            var affectedBlockOffsets = new ExplosionBlockOffset[affectedBlockOffsetsCount];
            for (int i = 0; i < affectedBlockOffsets.Length; i++)
                affectedBlockOffsets[i] = reader.ReadType<ExplosionBlockOffset>(protocolVersion);
            var playerMotionX = reader.ReadFloat();
            var playerMotionY = reader.ReadFloat();
            var playerMotionZ = reader.ReadFloat();
            return new ExplosionPacket(x, y, z, radius, affectedBlockOffsets, playerMotionX, playerMotionY, playerMotionZ, default!, default!, default!, default!, default!, default!);
        }

        if (protocolVersion >= 765 && protocolVersion <= 767)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            var radius = reader.ReadFloat();
            int affectedBlockOffsetsCount = reader.ReadVarInt();
            var affectedBlockOffsets = new ExplosionBlockOffset[affectedBlockOffsetsCount];
            for (int i = 0; i < affectedBlockOffsets.Length; i++)
                affectedBlockOffsets[i] = reader.ReadType<ExplosionBlockOffset>(protocolVersion);
            var playerMotionX = reader.ReadFloat();
            var playerMotionY = reader.ReadFloat();
            var playerMotionZ = reader.ReadFloat();
            var blockInteractionType = reader.ReadVarInt();
            var smallExplosionParticle = reader.ReadType<Particle>(protocolVersion);
            var largeExplosionParticle = reader.ReadType<Particle>(protocolVersion);
            var sound = reader.ReadType<ItemSoundHolder>(protocolVersion);
            return new ExplosionPacket(x, y, z, radius, affectedBlockOffsets, playerMotionX, playerMotionY, playerMotionZ, blockInteractionType, smallExplosionParticle, largeExplosionParticle, sound, default!, default!);
        }

        if (protocolVersion >= 768 && protocolVersion <= 768)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            Vec3f? playerKnockback = null;
            if (reader.ReadBoolean())
                playerKnockback = reader.ReadType<Vec3f>(protocolVersion);
            var explosionParticle = reader.ReadType<Particle>(protocolVersion);
            var sound = reader.ReadType<ItemSoundHolder>(protocolVersion);
            return new ExplosionPacket(x, y, z, default!, default!, default!, default!, default!, default!, default!, default!, sound, playerKnockback, explosionParticle);
        }

        if (protocolVersion >= 769)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            Vec3f64? playerKnockback = null;
            if (reader.ReadBoolean())
                playerKnockback = reader.ReadType<Vec3f64>(protocolVersion);
            var explosionParticle = reader.ReadType<Particle>(protocolVersion);
            var sound = reader.ReadType<ItemSoundHolder>(protocolVersion);
            return new ExplosionPacket(x, y, z, default!, default!, default!, default!, default!, default!, default!, default!, sound, playerKnockback, explosionParticle);
        }

        throw new System.NotSupportedException($"ExplosionPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<ExplosionPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            writer.WriteFloat((float)X);
            writer.WriteFloat((float)Y);
            writer.WriteFloat((float)Z);
            writer.WriteFloat(Radius);
            writer.WriteSignedInt((int)AffectedBlockOffsets.Length);
            foreach (var affectedBlockOffsetsItem in AffectedBlockOffsets)
                writer.WriteType<ExplosionBlockOffset>(affectedBlockOffsetsItem, protocolVersion);
            writer.WriteFloat(PlayerMotionX);
            writer.WriteFloat(PlayerMotionY);
            writer.WriteFloat(PlayerMotionZ);
            return;
        }

        if (protocolVersion >= 755 && protocolVersion <= 760)
        {
            writer.WriteFloat((float)X);
            writer.WriteFloat((float)Y);
            writer.WriteFloat((float)Z);
            writer.WriteFloat(Radius);
            writer.WriteVarInt(AffectedBlockOffsets.Length);
            foreach (var affectedBlockOffsetsItem in AffectedBlockOffsets)
                writer.WriteType<ExplosionBlockOffset>(affectedBlockOffsetsItem, protocolVersion);
            writer.WriteFloat(PlayerMotionX);
            writer.WriteFloat(PlayerMotionY);
            writer.WriteFloat(PlayerMotionZ);
            return;
        }

        if (protocolVersion >= 761 && protocolVersion <= 764)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteFloat(Radius);
            writer.WriteVarInt(AffectedBlockOffsets.Length);
            foreach (var affectedBlockOffsetsItem in AffectedBlockOffsets)
                writer.WriteType<ExplosionBlockOffset>(affectedBlockOffsetsItem, protocolVersion);
            writer.WriteFloat(PlayerMotionX);
            writer.WriteFloat(PlayerMotionY);
            writer.WriteFloat(PlayerMotionZ);
            return;
        }

        if (protocolVersion >= 765 && protocolVersion <= 767)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteFloat(Radius);
            writer.WriteVarInt(AffectedBlockOffsets.Length);
            foreach (var affectedBlockOffsetsItem in AffectedBlockOffsets)
                writer.WriteType<ExplosionBlockOffset>(affectedBlockOffsetsItem, protocolVersion);
            writer.WriteFloat(PlayerMotionX);
            writer.WriteFloat(PlayerMotionY);
            writer.WriteFloat(PlayerMotionZ);
            writer.WriteVarInt(BlockInteractionType);
            writer.WriteType<Particle>(SmallExplosionParticle, protocolVersion);
            writer.WriteType<Particle>(LargeExplosionParticle, protocolVersion);
            writer.WriteType<ItemSoundHolder>(Sound, protocolVersion);
            return;
        }

        if (protocolVersion >= 768 && protocolVersion <= 768)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteBoolean(PlayerKnockback is not null);
            if (PlayerKnockback is { } playerKnockbackValue)
                writer.WriteType<Vec3f>(playerKnockbackValue, protocolVersion);
            writer.WriteType<Particle>(ExplosionParticle, protocolVersion);
            writer.WriteType<ItemSoundHolder>(Sound, protocolVersion);
            return;
        }

        if (protocolVersion >= 769)
        {
            writer.WriteDouble(X);
            writer.WriteDouble(Y);
            writer.WriteDouble(Z);
            writer.WriteBoolean(PlayerKnockback is not null);
            if (PlayerKnockback is { } playerKnockbackValue)
                writer.WriteType<Vec3f64>(playerKnockbackValue, protocolVersion);
            writer.WriteType<Particle>(ExplosionParticle, protocolVersion);
            writer.WriteType<ItemSoundHolder>(Sound, protocolVersion);
            return;
        }

        throw new System.NotSupportedException($"ExplosionPacket has no wire layout for protocol version {protocolVersion}.");
    }
}
