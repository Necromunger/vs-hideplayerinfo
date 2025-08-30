using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HidePlayerInfo;

public class HidePlayerInfoModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.World.Config.SetBool("mapHideOtherPlayers", true);
        var harmony = new Harmony("necromunger.hideplayerinfo");
        harmony.PatchAll();
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