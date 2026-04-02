using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HidePlayerInfo;

public class HidePlayerInfoModSystem : ModSystem
{
    public static HidePlayerInfoModSystem Instance;
    public static string ConfigName = "HidePlayerInfoConfig.json";

    internal HidePlayerInfoConfig config;

    private Harmony harmony;
    private IServerNetworkChannel serverChannel;
    private PartyMapLayer partyMapLayer;

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

        if (config.AllowGroupMemberMapVisibility)
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
        mapManager.RegisterMapLayer<PartyMapLayer>("Party", 1.0);

        foreach (var layer in mapManager.MapLayers)
        {
            if (layer is PartyMapLayer pml)
            {
                partyMapLayer = pml;
                break;
            }
        }
    }

    private void OnPartyDataReceived(PartyMapData data)
    {
        partyMapLayer?.UpdatePositions(data.Positions);
    }

    private void SendPartyPositions(ICoreServerAPI sapi)
    {
        var allPlayers = sapi.World.AllOnlinePlayers;
        if (allPlayers.Length < 2) return;

        foreach (var player in allPlayers)
        {
            var serverPlayer = player as IServerPlayer;
            if (serverPlayer == null) continue;

            var myGroupIds = serverPlayer.Groups?
                .Select(g => g.GroupUid)
                .ToHashSet() ?? new HashSet<int>();

            if (myGroupIds.Count == 0)
            {
                serverChannel.SendPacket(new PartyMapData { Positions = System.Array.Empty<PartyMemberPosition>() }, serverPlayer);
                continue;
            }

            var members = new List<PartyMemberPosition>();

            foreach (var other in allPlayers)
            {
                if (other.PlayerUID == player.PlayerUID) continue;

                var otherServer = other as IServerPlayer;
                if (otherServer?.Groups == null) continue;

                bool shared = otherServer.Groups.Any(g => myGroupIds.Contains(g.GroupUid));
                if (!shared) continue;

                var pos = other.Entity?.Pos;
                if (pos == null) continue;

                members.Add(new PartyMemberPosition
                {
                    PlayerName = other.PlayerName,
                    X = pos.X,
                    Y = pos.Y,
                    Z = pos.Z
                });
            }

            serverChannel.SendPacket(new PartyMapData { Positions = members.ToArray() }, serverPlayer);
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
    static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
