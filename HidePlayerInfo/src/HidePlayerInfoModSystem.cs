using System.Collections.Generic;
using HarmonyLib;
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

    internal PartyMemberPosition[] partyPositions = [];
    private readonly HashSet<string> playersWithSentPartyData = new();

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
            sapi.Event.RegisterGameTickListener(dt => SendPartyPositions(sapi), 2000);
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
        partyPositions = data.Positions ?? [];
        partyMemberUids = [];
        foreach (var p in partyPositions)
            partyMemberUids.Add(p.PlayerUid);
    }

    private void SendPartyPositions(ICoreServerAPI sapi)
    {
        var allPlayers = sapi.World.AllOnlinePlayers;
        var groups = sapi.Groups.PlayerGroupsById;
        var onlinePlayerUids = new HashSet<string>();

        foreach (var player in allPlayers)
        {
            var serverPlayer = player as IServerPlayer;
            if (serverPlayer == null) continue;

            onlinePlayerUids.Add(serverPlayer.PlayerUID);

            // skip players not in any group
            if (serverPlayer.Groups == null || serverPlayer.Groups.Length == 0)
            {
                SendEmptyPartyDataIfNeeded(serverPlayer);
                continue;
            }

            var seen = new HashSet<string>();
            var members = new List<PartyMemberPosition>();

            // search all player groups for players
            foreach (var playerGroups in serverPlayer.Groups)
            {
                if (!groups.TryGetValue(playerGroups.GroupUid, out var group)) continue;
                if (group.OnlinePlayers == null) continue;

                foreach (var member in group.OnlinePlayers)
                {
                    if (member.PlayerUID == player.PlayerUID) continue;
                    if (member.GetGroup(group.Uid) == null) continue;

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

            if (members.Count == 0)
            {
                SendEmptyPartyDataIfNeeded(serverPlayer);
                continue;
            }

            serverChannel.SendPacket(new PartyMapData { Positions = [.. members] }, serverPlayer);
            playersWithSentPartyData.Add(serverPlayer.PlayerUID);
        }

        playersWithSentPartyData.RemoveWhere(playerUid => !onlinePlayerUids.Contains(playerUid));
    }

    private void SendEmptyPartyDataIfNeeded(IServerPlayer serverPlayer)
    {
        if (!playersWithSentPartyData.Remove(serverPlayer.PlayerUID))
            return;

        serverChannel.SendPacket(new PartyMapData { Positions = [] }, serverPlayer);
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

#region Harmony patches

static class NameTagPatchUtils
{
    static readonly System.Reflection.FieldInfo RenderRangeField =
        AccessTools.Field(typeof(EntityBehaviorNameTag), "renderRange");

    internal static bool HasVisiblePartyNametag(EntityBehaviorNameTag nameTag)
    {
        return HidePlayerInfoModSystem.Instance?.config?.AllowGroupMemberNametagVisibility == true
            && nameTag.entity is EntityPlayer ep
            && HidePlayerInfoModSystem.Instance.partyMemberUids.Contains(ep.PlayerUID);
    }

    internal static int GetDesiredRenderRange(EntityBehaviorNameTag nameTag)
    {
        if (HasVisiblePartyNametag(nameTag))
            return 999;

        return HidePlayerInfoModSystem.Instance?.config?.NametagVisibilityDistance ?? 999;
    }

    internal static void SyncRenderRangeField(EntityBehaviorNameTag nameTag)
    {
        // Some builds read the backing field directly during render instead of the property getter.
        RenderRangeField?.SetValue(nameTag, GetDesiredRenderRange(nameTag));
    }
}

[HarmonyPatch(typeof(EntityBehaviorNameTag), nameof(EntityBehaviorNameTag.OnRenderFrame))]
class Patch_EntityBehaviorNameTag_OnRenderFrame
{
    static readonly System.Reflection.MethodInfo IsSelf = AccessTools.PropertyGetter(typeof(EntityBehaviorNameTag), "IsSelf");
    static bool Prefix(EntityBehaviorNameTag __instance, float deltaTime, EnumRenderStage stage)
    {
        NameTagPatchUtils.SyncRenderRangeField(__instance);

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
        if (NameTagPatchUtils.HasVisiblePartyNametag(__instance))
        {
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(EntityBehaviorNameTag), nameof(EntityBehaviorNameTag.RenderRange), MethodType.Getter)]
class Patch_RenderRange_Get
{
    static bool Prefix(EntityBehaviorNameTag __instance, ref int __result)
    {
        __result = NameTagPatchUtils.GetDesiredRenderRange(__instance);
        return false;
    }
}

#endregion