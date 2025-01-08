namespace SandBoxLib;



public class BlockPlace : IClientPacket
{
    public Position Location { get; set; }
    public int Direction { get; set; }
    public int Hand { get; set; }
    public float CursorX { get; set; }
    public float CursorY { get; set; }
    public float CursorZ { get; set; }

    public sealed class V340_404 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 340 and <= 404;
        }
    }

    public sealed class V477_758 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 477 and <= 758;
        }

        public bool InsideBlock { get; set; }
    }

    public sealed class V759_767 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 759 and <= 767;
        }

        public bool InsideBlock { get; set; }
        public int Sequence { get; set; }
    }

    public sealed class V768 : BlockPlace
    {
        public static new bool SupportedVersion(int protocolVersion)
        {
            return protocolVersion is >= 768 and <= 768;
        }

        public bool InsideBlock { get; set; }
        public bool WorldBorderHit { get; set; }
        public int Sequence { get; set; }
    }

    public void Serialize(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        throw new NotImplementedException();
    }
}

public static class BlockPlaceExtenstion
{
    public static void AllHandler(this BlockPlace packet, Action<BlockPlace.V340_404> func1,
        Action<BlockPlace.V477_758> func2, Action<BlockPlace.V759_767> func3, Action<BlockPlace.V768> func4)
    {
        if (packet is BlockPlace.V340_404 p1)
            func1.Invoke(p1);
        else if (packet is BlockPlace.V477_758 p2)
            func2.Invoke(p2);
        else if (packet is BlockPlace.V759_767 p3)
            func3.Invoke(p3);
        else if (packet is BlockPlace.V768 p4)
            func4.Invoke(p4);
    }
}