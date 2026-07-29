using ProtoBuf;

namespace Burdened.Network;

[ProtoContract]
public sealed class PickupFloorBagPacket
{
    [ProtoMember(1)]
    public int X;

    [ProtoMember(2)]
    public int Y;

    [ProtoMember(3)]
    public int Z;

    /// <summary>
    /// Which slot of the ground storage the player pointed at.
    /// </summary>
    [ProtoMember(4)]
    public int SlotIndex;
}
