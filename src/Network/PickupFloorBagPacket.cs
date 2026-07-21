using ProtoBuf;

namespace Burdened.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public sealed class PickupFloorBagPacket
{
    public int X;
    public int Y;
    public int Z;
}
