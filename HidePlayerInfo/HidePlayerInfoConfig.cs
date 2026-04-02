using Vintagestory.API.Common;

namespace HidePlayerInfo;

public class HidePlayerInfoConfig
{
    public bool AllowGroupMemberMapVisibility = false;
    public bool AllowGroupMemberNametagVisibility = false;

    public static HidePlayerInfoConfig GetDefault(ICoreAPI api)
    {
        var cfg = new HidePlayerInfoConfig();
        api.StoreModConfig(cfg, HidePlayerInfoModSystem.ConfigName);
        return cfg;
    }
}
