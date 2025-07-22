using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HidePlayerInfo;

public class HidePlayerInfoModSystem : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.World.Config.SetBool("mapHideOtherPlayers", true);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        var harmony = new Harmony("necromunger.hideplayerinfo");
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(WorldMapManager), nameof(WorldMapManager.SendMapDataToClient))]
class Patch_SendMapDataToClient
{
    static bool Prefix(WorldMapManager map, MapLayer forMapLayer, IServerPlayer forPlayer, byte[] data)
    {
        if (forMapLayer is PlayerMapLayer)
            return false;

        return true;
    }
}
