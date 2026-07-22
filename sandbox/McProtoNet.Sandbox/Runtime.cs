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
}

namespace McProtoNet.Protocol
{
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

namespace McProtoNet.Serialization
{
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
    }
}
