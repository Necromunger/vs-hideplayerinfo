using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace HidePlayerInfo;

public class HidePlayerInfoModSystem : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Event.LevelFinalize += () =>
        {
            var map = api.ModLoader.GetModSystem<WorldMapManager>();
            map.MapLayerRegistry.Remove("players");
            map.LayerGroupPositions.Remove("players");
            map.MapLayers.RemoveAll(l => l is PlayerMapLayer);
        };
    }
}
