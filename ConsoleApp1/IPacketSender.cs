public interface IPacketSender
{
    ValueTask SendPacket(MemoryOwner<byte> getWrittenMemory);
}