using ProtoBuf;

namespace Burdened.Network;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public sealed class PlaceEquippedBagPacket
{
    public int BagIndex;
    public int X;
    public int Y;
    public int Z;
    public int FaceIndex;
    public double HitX;
    public double HitY;
    public double HitZ;
}
