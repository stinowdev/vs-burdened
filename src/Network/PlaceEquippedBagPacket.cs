using ProtoBuf;

namespace Burdened.Network;

[ProtoContract]
public sealed class PlaceEquippedBagPacket
{
    [ProtoMember(1)]
    public int BagIndex;

    [ProtoMember(2)]
    public int X;

    [ProtoMember(3)]
    public int Y;

    [ProtoMember(4)]
    public int Z;

    [ProtoMember(5)]
    public int FaceIndex;

    [ProtoMember(6)]
    public double HitX;

    [ProtoMember(7)]
    public double HitY;

    [ProtoMember(8)]
    public double HitZ;
}
