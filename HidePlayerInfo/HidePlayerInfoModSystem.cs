using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HidePlayerInfo;

public class HidePlayerInfoModSystem : ModSystem
{
    public static HidePlayerInfoModSystem Instance;
    public static string ConfigName = "HidePlayerInfo.json";

    internal HidePlayerInfoConfig config;
    internal HashSet<string> partyMemberUids = new();

    internal PartyMemberPosition[] partyPositions = System.Array.Empty<PartyMemberPosition>();

    private Harmony harmony;
    private IServerNetworkChannel serverChannel;

    public override void Start(ICoreAPI api)
    {
        Instance = this;

        config = api.LoadModConfig<HidePlayerInfoConfig>(ConfigName) ?? HidePlayerInfoConfig.GetDefault(api);

        api.World.Config.SetBool("mapHideOtherPlayers", true);

        harmony = new Harmony("necromunger.hideplayerinfo");
        harmony.PatchAll();
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        serverChannel = sapi.Network.RegisterChannel("hideplayerinfo")
            .RegisterMessageType<PartyMapData>();

        if (config.AllowGroupMemberMapVisibility || config.AllowGroupMemberNametagVisibility)
        {
            sapi.Event.RegisterGameTickListener(dt => SendPartyPositions(sapi), 2000);
        }
    }

    public override void StartClientSide(ICoreClientAPI capi)
    {
        capi.Network.RegisterChannel("hideplayerinfo")
            .RegisterMessageType<PartyMapData>()
            .SetMessageHandler<PartyMapData>(OnPartyDataReceived);

        var mapManager = capi.ModLoader.GetModSystem<WorldMapManager>();
        mapManager.RegisterMapLayer<PartyMapLayer>("Group", 1.0);
    }

    private void OnPartyDataReceived(PartyMapData data)
    {
        partyPositions = data.Positions ?? System.Array.Empty<PartyMemberPosition>();

        var uids = new HashSet<string>();
        foreach (var p in partyPositions)
        {
            if (p.PlayerUid != null) uids.Add(p.PlayerUid);
        }
        partyMemberUids = uids;
    }

    private void SendPartyPositions(ICoreServerAPI sapi)
    {
        var allPlayers = sapi.World.AllOnlinePlayers;
        if (allPlayers.Length < 2) return;
        var groupsById = sapi.Groups.PlayerGroupsById;

        foreach (var player in allPlayers)
        {
            var serverPlayer = player as IServerPlayer;
            if (serverPlayer == null) continue;

            var seen = new HashSet<string>();
            var members = new List<PartyMemberPosition>();

            if (serverPlayer.Groups != null)
            foreach (var membership in serverPlayer.Groups)
            {
                if (!groupsById.TryGetValue(membership.GroupUid, out var group)) continue;
                if (group.OnlinePlayers == null) continue;

                foreach (var member in group.OnlinePlayers)
                {
                    if (member.PlayerUID == player.PlayerUID) continue;
                    if (!seen.Add(member.PlayerUID)) continue;

                    var pos = member.Entity?.Pos;
                    if (pos == null) continue;

                    members.Add(new PartyMemberPosition
                    {
                        PlayerName = member.PlayerName,
                        PlayerUid = member.PlayerUID,
                        X = pos.X,
                        Y = pos.Y,
                        Z = pos.Z
                    });
                }
            }

            serverChannel.SendPacket(new PartyMapData { Positions = [.. members] }, serverPlayer);
        }
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll("necromunger.hideplayerinfo");
        Instance = null;
    }
}

[HarmonyPatch(typeof(WorldMapManager), nameof(WorldMapManager.SendMapDataToClient))]
class Patch_SendMapDataToClient
{
    static bool Prefix(WorldMapManager __instance, MapLayer forMapLayer, IServerPlayer forPlayer, byte[] data)
    {
        if (forMapLayer is PlayerMapLayer)
            return false;

        return true;
    }
}

[HarmonyPatch(typeof(EntityBehaviorNameTag), nameof(EntityBehaviorNameTag.OnRenderFrame))]
class Patch_EntityBehaviorNameTag_OnRenderFrame
{
    static readonly System.Reflection.MethodInfo IsSelf = AccessTools.PropertyGetter(typeof(EntityBehaviorNameTag), "IsSelf");
    static bool Prefix(EntityBehaviorNameTag __instance, float deltaTime, EnumRenderStage stage)
    {
        bool isSelf = (bool)IsSelf.Invoke(__instance, null);
        if (isSelf)
            return false;

        return true;
    }
}

[HarmonyPatch(typeof(EntityBehaviorNameTag), nameof(EntityBehaviorNameTag.ShowOnlyWhenTargeted), MethodType.Getter)]
class Patch_ShowOnlyWhenTargeted_Get
{
    static bool Prefix(EntityBehaviorNameTag __instance, ref bool __result)
    {
        // Party members get visible nametags (don't force ShowOnlyWhenTargeted)
        if (HidePlayerInfoModSystem.Instance?.config?.AllowGroupMemberNametagVisibility == true
            && __instance.entity is EntityPlayer ep
            && HidePlayerInfoModSystem.Instance.partyMemberUids.Contains(ep.PlayerUID))
        {
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }
}
