// Minimal McProtoNet-shaped runtime so the generated protocol types compile and round-trip.
// This is a sandbox model of the real McProtoNet surface, not the real thing.

using System;
using System.Collections.Generic;
using System.Text;

namespace McProtoNet.Protocol.Attributes
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = true)]
    public sealed class ProtocolSupportAttribute : Attribute
    {
        public int From { get; }
        public int To { get; }
        public ProtocolSupportAttribute(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class PacketAttribute : Attribute
    {
        public string Key { get; }
        public McProtoNet.Protocol.PacketPhase Phase { get; }
        public McProtoNet.Protocol.PacketDirection Direction { get; }

        public PacketAttribute(string key, McProtoNet.Protocol.PacketPhase phase,
            McProtoNet.Protocol.PacketDirection direction)
        {
            Key = key;
            Phase = phase;
            Direction = direction;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class PacketFieldAttribute : Attribute
    {
        public string Name { get; }
        public string TypeName { get; }
        public string? Group { get; init; }
        public int From { get; init; }
        public int To { get; init; }

        public PacketFieldAttribute(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }
}

namespace McProtoNet.Protocol
{
    using McProtoNet.Serialization;

    public enum PacketPhase : byte { Handshaking, Status, Login, Configuration, Play }

    public enum PacketDirection : byte { Clientbound, Serverbound }

    public readonly record struct PacketIdentity(
        string Key, string Name, PacketPhase Phase, PacketDirection Direction, ushort Ordinal);

    // Sandbox mirror of the real non-generic IPacket: what a decoded packet answers once its
    // static type is gone. Generated packets implement it explicitly
    // (`PacketIdentity IPacket.Identity => Identity;`), which is why it does not clash with the
    // static abstract member on the generic one below — and why the generic one must not inherit it.
    public interface IPacket
    {
        PacketIdentity Identity { get; }
    }

    // Sandbox mirror of the real IPacket: no class constraint, so it serves both the current
    // record-struct packets and the form-A classes.
    public interface IPacket<TSelf> : IProtocolType<TSelf> where TSelf : IPacket<TSelf>
    {
        static abstract PacketIdentity Identity { get; }
        static abstract bool TryGetPacketId(int protocolVersion, out int id);
    }

    public sealed class WrongLayerException : Exception
    {
        public WrongLayerException(string packetName, int protocolVersion, string expectedLayer)
            : base($"{packetName}: protocol {protocolVersion} requires layer {expectedLayer}, but it is null.")
        {
        }
    }

    public static class MinecraftVersion
    {
        public const int StartProtocol = 735;   // 1.16
        public const int LatestProtocol = 772;  // 1.21.7 / 1.21.8
    }

    public static class ThrowHelper
    {
        public static void ThrowIfProtocolNotSupported<T>(int protocolVersion)
        {
            var attrs = typeof(T).GetCustomAttributes(typeof(Attributes.ProtocolSupportAttribute), false);
            if (attrs.Length == 0) return;
            foreach (Attributes.ProtocolSupportAttribute a in attrs)
                if (protocolVersion >= a.From && protocolVersion <= a.To) return;
            throw new InvalidOperationException(
                $"{typeof(T).Name} is not supported at protocol version {protocolVersion}.");
        }
    }
}

namespace McProtoNet.Protocol
{
    using McProtoNet.Serialization;

    // Hand-written runtime primitive (mirrors McProtoNet): block position packed into one long
    // (x: 26 bits, z: 26 bits, y: 12 bits). Referenced from specs as Named "Position", never generated.
    public readonly partial record struct Position(int X, int Y, int Z) : IProtocolType<Position>
    {
        public static Position Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
        {
            var v = reader.ReadSignedLong();
            int x = (int)(v >> 38);
            int y = (int)(v << 52 >> 52);
            int z = (int)(v << 26 >> 38);
            return new Position(x, y, z);
        }

        public readonly void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
        {
            long v = ((long)(X & 0x3FFFFFF) << 38) | ((long)(Z & 0x3FFFFFF) << 12) | ((long)Y & 0xFFF);
            writer.WriteSignedLong(v);
        }
    }
}

namespace McProtoNet.Serialization
{
    // Contract every generated protocol type implements. Static abstract members give
    // ReadType<T>/WriteType<T> zero-reflection dispatch: `T.Read(...)` compiles to a direct
    // (devirtualized for structs) call.
    public interface IProtocolType<TSelf> where TSelf : IProtocolType<TSelf>
    {
        static abstract TSelf Read(ref MinecraftPrimitiveReader reader, int protocolVersion);
        void Write(MinecraftPrimitiveWriter writer, int protocolVersion);
    }

    // Big-endian primitive reader over a byte buffer. A struct so the generated
    // `Read(ref MinecraftPrimitiveReader reader, ...)` signature works.
    public struct MinecraftPrimitiveReader
    {
        private readonly byte[] _data;
        private int _pos;

        public MinecraftPrimitiveReader(byte[] data)
        {
            _data = data;
            _pos = 0;
        }

        public int Position => _pos;

        private byte Next() => _data[_pos++];

        public bool ReadBoolean() => Next() != 0;
        public byte ReadUnsignedByte() => Next();
        public sbyte ReadSignedByte() => (sbyte)Next();

        public ushort ReadUnsignedShort() => (ushort)((Next() << 8) | Next());
        public short ReadSignedShort() => (short)ReadUnsignedShort();

        public uint ReadUnsignedInt()
        {
            uint v = 0;
            for (int i = 0; i < 4; i++) v = (v << 8) | Next();
            return v;
        }
        public int ReadSignedInt() => (int)ReadUnsignedInt();

        public ulong ReadUnsignedLong()
        {
            ulong v = 0;
            for (int i = 0; i < 8; i++) v = (v << 8) | Next();
            return v;
        }
        public long ReadSignedLong() => (long)ReadUnsignedLong();

        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadSignedInt());
        public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadSignedLong());

        public int ReadVarInt()
        {
            int value = 0, shift = 0;
            byte b;
            do
            {
                b = Next();
                value |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return value;
        }

        public long ReadVarLong()
        {
            long value = 0;
            int shift = 0;
            byte b;
            do
            {
                b = Next();
                value |= (long)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return value;
        }

        public string ReadString()
        {
            int len = ReadVarInt();
            var s = Encoding.UTF8.GetString(_data, _pos, len);
            _pos += len;
            return s;
        }

        public Guid ReadUUID()
        {
            var hi = ReadUnsignedLong();
            var lo = ReadUnsignedLong();
            Span<byte> b = stackalloc byte[16];
            for (int i = 0; i < 8; i++) b[i] = (byte)(hi >> ((7 - i) * 8));
            for (int i = 0; i < 8; i++) b[8 + i] = (byte)(lo >> ((7 - i) * 8));
            return new Guid(b);
        }

        public byte[] ReadByteArray()
        {
            int len = ReadVarInt();
            var b = new byte[len];
            Array.Copy(_data, _pos, b, 0, len);
            _pos += len;
            return b;
        }

        public byte[] ReadRestBytes()
        {
            var b = new byte[_data.Length - _pos];
            Array.Copy(_data, _pos, b, 0, b.Length);
            _pos = _data.Length;
            return b;
        }

        // Nested named-type dispatch without reflection: static abstract interface member.
        public T ReadType<T>(int protocolVersion) where T : IProtocolType<T>
            => T.Read(ref this, protocolVersion);
    }

    // Big-endian primitive writer collecting into a growable buffer.
    public sealed class MinecraftPrimitiveWriter
    {
        private readonly List<byte> _buf = new();

        public byte[] ToArray() => _buf.ToArray();

        public void WriteBoolean(bool v) => _buf.Add(v ? (byte)1 : (byte)0);
        public void WriteUnsignedByte(byte v) => _buf.Add(v);
        public void WriteSignedByte(sbyte v) => _buf.Add((byte)v);

        public void WriteUnsignedShort(ushort v)
        {
            _buf.Add((byte)(v >> 8));
            _buf.Add((byte)v);
        }
        public void WriteSignedShort(short v) => WriteUnsignedShort((ushort)v);

        public void WriteUnsignedInt(uint v)
        {
            for (int i = 3; i >= 0; i--) _buf.Add((byte)(v >> (i * 8)));
        }
        public void WriteSignedInt(int v) => WriteUnsignedInt((uint)v);

        public void WriteUnsignedLong(ulong v)
        {
            for (int i = 7; i >= 0; i--) _buf.Add((byte)(v >> (i * 8)));
        }
        public void WriteSignedLong(long v) => WriteUnsignedLong((ulong)v);

        public void WriteFloat(float v) => WriteSignedInt(BitConverter.SingleToInt32Bits(v));
        public void WriteDouble(double v) => WriteSignedLong(BitConverter.DoubleToInt64Bits(v));

        public void WriteVarInt(int value)
        {
            uint v = (uint)value;
            while ((v & ~0x7Fu) != 0)
            {
                _buf.Add((byte)((v & 0x7F) | 0x80));
                v >>= 7;
            }
            _buf.Add((byte)v);
        }

        public void WriteVarLong(long value)
        {
            ulong v = (ulong)value;
            while ((v & ~0x7Ful) != 0)
            {
                _buf.Add((byte)((v & 0x7F) | 0x80));
                v >>= 7;
            }
            _buf.Add((byte)v);
        }

        public void WriteString(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            WriteVarInt(bytes.Length);
            _buf.AddRange(bytes);
        }

        public void WriteUUID(Guid g)
        {
            var b = g.ToByteArray();
            _buf.AddRange(b);
        }

        public void WriteByteArray(byte[] v)
        {
            WriteVarInt(v.Length);
            _buf.AddRange(v);
        }

        public void WriteRestBytes(byte[] v) => _buf.AddRange(v);

        // Mirror of MinecraftPrimitiveReader.ReadType<T>: direct interface dispatch, no reflection.
        public void WriteType<T>(T value, int protocolVersion) where T : IProtocolType<T>
            => value.Write(this, protocolVersion);
    }
}
