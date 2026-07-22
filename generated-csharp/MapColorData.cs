using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol;

[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class MapColorData
{
    public int Rows { get; }
    public int X { get; }
    public int Y { get; }
    public byte[] Data { get; }

    public MapColorData(int rows, int x, int y, byte[] data)
    {
        Rows = rows;
        X = x;
        Y = y;
        Data = data;
    }

    public static MapColorData Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<MapColorData>(protocolVersion);
        var rows = reader.ReadSignedByte();
        var x = reader.ReadSignedByte();
        var y = reader.ReadSignedByte();
        // TODO(codegen): read 'Data' (ByteArray)
        return new MapColorData(rows, x, y, data);
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<MapColorData>(protocolVersion);
        writer.WriteSignedByte(Rows);
        writer.WriteSignedByte(X);
        writer.WriteSignedByte(Y);
        // TODO(codegen): write 'Data' (ByteArray)
    }
}
