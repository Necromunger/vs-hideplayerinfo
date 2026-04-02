using ProtoBuf;

namespace HidePlayerInfo;

[ProtoContract]
public class PartyMapData
{
    [ProtoMember(1)]
    public PartyMemberPosition[] Positions;
}

[ProtoContract]
public class PartyMemberPosition
{
    [ProtoMember(1)]
    public string PlayerName;

    [ProtoMember(2)]
    public double X;

    [ProtoMember(3)]
    public double Y;

    [ProtoMember(4)]
    public double Z;
}
