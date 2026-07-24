using McProtoNet.Protocol.Attributes;
using McProtoNet.Serialization;

namespace McProtoNet.Protocol.Packets.Play.Serverbound;
[ProtocolSupport(MinecraftVersion.StartProtocol, MinecraftVersion.LatestProtocol)]
public sealed partial class WindowClickPacket : IProtocolType<WindowClickPacket>
{
    public int WindowId { get; }
    public int StateId { get; }
    public int Slot { get; }
    public int MouseButton { get; }
    public int Mode { get; }
    public Slot CursorItem { get; }

    public WindowClickPacket(int windowId, int stateId, int slot, int mouseButton, int mode, Slot cursorItem)
    {
        WindowId = windowId;
        StateId = stateId;
        Slot = slot;
        MouseButton = mouseButton;
        Mode = mode;
        CursorItem = cursorItem;
    }

    public static WindowClickPacket Read(ref MinecraftPrimitiveReader reader, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<WindowClickPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            var windowId = reader.ReadUnsignedByte();
            var slot = reader.ReadSignedShort();
            var mouseButton = reader.ReadSignedByte();
            reader.ReadSignedShort();
            var mode = reader.ReadSignedByte();
            reader.ReadType<Slot>(protocolVersion);
            var cursorItem = reader.ReadType<Slot>(protocolVersion);
            return new WindowClickPacket(windowId, default!, slot, mouseButton, mode, cursorItem);
        }

        if (protocolVersion >= 755 && protocolVersion <= 755)
        {
            var windowId = reader.ReadUnsignedByte();
            var slot = reader.ReadSignedShort();
            var mouseButton = reader.ReadSignedByte();
            var mode = reader.ReadSignedByte();
            int skipChangedSlotsCount = reader.ReadVarInt();
            for (int i = 0; i < skipChangedSlotsCount; i++)
                reader.ReadType<ChangedSlot>(protocolVersion);
            var cursorItem = reader.ReadType<Slot>(protocolVersion);
            return new WindowClickPacket(windowId, default!, slot, mouseButton, mode, cursorItem);
        }

        if (protocolVersion >= 756 && protocolVersion <= 765)
        {
            var windowId = reader.ReadUnsignedByte();
            var stateId = reader.ReadVarInt();
            var slot = reader.ReadSignedShort();
            var mouseButton = reader.ReadSignedByte();
            var mode = reader.ReadVarInt();
            int skipChangedSlotsCount = reader.ReadVarInt();
            for (int i = 0; i < skipChangedSlotsCount; i++)
                reader.ReadType<ChangedSlot>(protocolVersion);
            var cursorItem = reader.ReadType<Slot>(protocolVersion);
            return new WindowClickPacket(windowId, stateId, slot, mouseButton, mode, cursorItem);
        }

        if (protocolVersion >= 770)
        {
            var windowId = reader.ReadUnsignedByte();
            var stateId = reader.ReadVarInt();
            var slot = reader.ReadSignedShort();
            var mouseButton = reader.ReadSignedByte();
            var mode = reader.ReadVarInt();
            int skipChangedSlotsCount = reader.ReadVarInt();
            for (int i = 0; i < skipChangedSlotsCount; i++)
                reader.ReadType<ChangedSlot>(protocolVersion);
            HashedSlot? cursorItem = null;
            if (reader.ReadBoolean())
                cursorItem = reader.ReadType<HashedSlot>(protocolVersion);
            return new WindowClickPacket(windowId, stateId, slot, mouseButton, mode, cursorItem);
        }

        throw new System.NotSupportedException($"WindowClickPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public void Write(MinecraftPrimitiveWriter writer, int protocolVersion)
    {
        ThrowHelper.ThrowIfProtocolNotSupported<WindowClickPacket>(protocolVersion);
        if (protocolVersion <= 754)
        {
            writer.WriteUnsignedByte((byte)WindowId);
            writer.WriteSignedShort((short)Slot);
            writer.WriteSignedByte((sbyte)MouseButton);
            writer.WriteSignedShort(default);
            writer.WriteSignedByte((sbyte)Mode);
            writer.WriteType<Slot>(default, protocolVersion);
            writer.WriteType<Slot>(CursorItem, protocolVersion);
            return;
        }

        if (protocolVersion >= 755 && protocolVersion <= 755)
        {
            writer.WriteUnsignedByte((byte)WindowId);
            writer.WriteSignedShort((short)Slot);
            writer.WriteSignedByte((sbyte)MouseButton);
            writer.WriteSignedByte((sbyte)Mode);
            writer.WriteVarInt(0);
            writer.WriteType<Slot>(CursorItem, protocolVersion);
            return;
        }

        if (protocolVersion >= 756 && protocolVersion <= 765)
        {
            writer.WriteUnsignedByte((byte)WindowId);
            writer.WriteVarInt(StateId);
            writer.WriteSignedShort((short)Slot);
            writer.WriteSignedByte((sbyte)MouseButton);
            writer.WriteVarInt(Mode);
            writer.WriteVarInt(0);
            writer.WriteType<Slot>(CursorItem, protocolVersion);
            return;
        }

        if (protocolVersion >= 770)
        {
            writer.WriteUnsignedByte((byte)WindowId);
            writer.WriteVarInt(StateId);
            writer.WriteSignedShort((short)Slot);
            writer.WriteSignedByte((sbyte)MouseButton);
            writer.WriteVarInt(Mode);
            writer.WriteVarInt(0);
            writer.WriteBoolean(CursorItem is not null);
            if (CursorItem is { } cursorItemValue)
                writer.WriteType<HashedSlot>(cursorItemValue, protocolVersion);
            return;
        }

        throw new System.NotSupportedException($"WindowClickPacket has no wire layout for protocol version {protocolVersion}.");
    }

    public static int GetPacketId(int protocolVersion)
    {
        if (protocolVersion >= 735 && protocolVersion <= 736)
            return 0x09;
        if (protocolVersion >= 751 && protocolVersion <= 754)
            return 0x09;
        if (protocolVersion >= 755 && protocolVersion <= 758)
            return 0x08;
        if (protocolVersion >= 759 && protocolVersion <= 759)
            return 0x0A;
        if (protocolVersion >= 760 && protocolVersion <= 760)
            return 0x0B;
        if (protocolVersion >= 761 && protocolVersion <= 761)
            return 0x0A;
        if (protocolVersion >= 762 && protocolVersion <= 763)
            return 0x0B;
        if (protocolVersion >= 764 && protocolVersion <= 764)
            return 0x0D;
        if (protocolVersion >= 765 && protocolVersion <= 765)
            return 0x0D;
        if (protocolVersion >= 766 && protocolVersion <= 767)
            return 0x0E;
        if (protocolVersion >= 768 && protocolVersion <= 768)
            return 0x10;
        if (protocolVersion >= 769 && protocolVersion <= 769)
            return 0x10;
        if (protocolVersion >= 770 && protocolVersion <= 770)
            return 0x10;
        if (protocolVersion >= 771 && protocolVersion <= 772)
            return 0x11;
        throw new System.NotSupportedException($"No packet id for protocol {protocolVersion}.");
    }
}
