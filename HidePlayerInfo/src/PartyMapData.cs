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
    public string PlayerUid;

    [ProtoMember(3)]
    public double X;

    [ProtoMember(4)]
    public double Y;

    [ProtoMember(5)]
    public double Z;
}
