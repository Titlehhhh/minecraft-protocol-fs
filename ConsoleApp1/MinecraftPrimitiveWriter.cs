using System.Numerics;

public class MinecraftPrimitiveWriter : IMinecraftPrimitiveWriter
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void WriteBoolean(bool value)
    {
        throw new NotImplementedException();
    }

    public void WriteSignedByte(sbyte value)
    {
        throw new NotImplementedException();
    }

    public void WriteUnsignedByte(byte value)
    {
        throw new NotImplementedException();
    }

    public void WriteUnsignedShort(ushort value)
    {
        throw new NotImplementedException();
    }

    public void WriteSignedShort(short value)
    {
        throw new NotImplementedException();
    }

    public void WriteSignedInt(int value)
    {
        throw new NotImplementedException();
    }

    public void WriteUnsignedInt(uint value)
    {
        throw new NotImplementedException();
    }

    public void WriteSignedLong(long value)
    {
        throw new NotImplementedException();
    }

    public void WriteUnsignedLong(ulong value)
    {
        throw new NotImplementedException();
    }

    public void WriteFloat(float value)
    {
        throw new NotImplementedException();
    }

    public void WriteDouble(double value)
    {
        throw new NotImplementedException();
    }

    public void WriteUUID(Guid value)
    {
        throw new NotImplementedException();
    }

    public void WriteBuffer(ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public void WriteVarInt(int? value)
    {
        throw new NotImplementedException();
    }

    public void WriteVarInt(int value)
    {
        throw new NotImplementedException();
    }

    public void WriteVarLong(long value)
    {
        throw new NotImplementedException();
    }

    public void WriteString(string value)
    {
        throw new NotImplementedException();
    }

    public void WriteOptionalNbt(NbtTag? value)
    {
        throw new NotImplementedException();
    }

    public void WriteNbt(NbtTag value)
    {
        throw new NotImplementedException();
    }

    public void WriteOptionalNbt(NbtTag? value, bool anon)
    {
        throw new NotImplementedException();
    }
    
    public void WriteNbt(NbtTag value, bool anon)
    {
        throw new NotImplementedException();
    }
    public void WriteVector2f(Vector2 o)
    {
        throw new NotImplementedException();
    }
    
    public void WriteVector3f(Vector3 o)
    {
        throw new NotImplementedException();
    }
    public void WriteVector4f(Vector4 o)
    {
        throw new NotImplementedException();
    }

    public void WritePosition(Position pos)
    {
        
    }
    
    public void WriteVector3F64 (Vector3F4 o)
    {
        
    }
    
    public MemoryOwner<byte> GetWrittenMemory()
    {
        throw new NotImplementedException();
    }
}

public class Position
{
}

public class Vector3F4
{
}