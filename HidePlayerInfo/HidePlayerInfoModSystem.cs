using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using VSSurvivalMod;
using JsonObject = Vintagestory.API.Datastructures.JsonObject;

namespace HidePlayerInfo;

public class HidePlayerInfoModSystem : ModSystem
{
    private HidePlayersConfig config;

    public override void AssetsFinalize(ICoreAPI api)
    {
        config = api.LoadModConfig<HidePlayersConfig>("HidePlayers.json") ?? HidePlayersConfig.GetDefault(api);
        foreach (EntityProperties entity in api.World.EntityTypes)
        {
            // Make sure collectible or its code is not null
            if (entity == null || entity.Code == null)
                continue;

            if (entity.Class != "EntityPlayer")
                continue;

            var beh = entity.Client.BehaviorsAsJsonObj;

            //var tag = new EntityBehaviorNameTag()
            // Make sure attribute exists
            //if (entity.Client.BehaviorsAsJsonObj != null && entity.Client.BehaviorsAsJsonObj[1] != null)
            //{
            // Retrieve list
            //    var obj = entity.Client.BehaviorsAsJsonObj[1].AsObject<Object>;

            //    obj["showtagonlywhentargeted"] = true;

            // Add new value
            //    list.Add(1);

            // Put it back
            //    entity.Attributes.Token["somelist"] = JToken.FromObject(list);
            //}

            //nameTagBehaviour["showtagonlywhentargeted"] = new JsonPrimitive(true);

            //EntityBehaviorNameTag nametagBehavior = behaviour?.FirstOrDefault() as EntityBehaviorNameTag;
            //nametagBehavior.ShowOnlyWhenTargeted = true;
            //nametagBehavior.RenderRange = config.RenderRange;
        }
    }

    public override void Start(ICoreAPI api)
    {
        //config = api.LoadModConfig<HidePlayersConfig>("HidePlayersConfig.json") ?? HidePlayersConfig.GetDefault(api);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }
}

public class HidePlayersConfig
{
    public int RenderRange = 20;

    public static HidePlayersConfig GetDefault(ICoreAPI api)
    {
        var cfg = new HidePlayersConfig();
        api.StoreModConfig(cfg, "HidePlayers.json");
        return cfg;
    }
}
