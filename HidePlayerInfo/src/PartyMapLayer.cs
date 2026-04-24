using Cairo;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HidePlayerInfo;

public class PartyMapLayer : MapLayer
{
    private readonly ICoreClientAPI capi;
    private LoadedTexture pinTex;

    public override string Title => "Group";
    public override string LayerGroupCode => "group";
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    public override bool RequireChunkLoaded => false;

    public PartyMapLayer(ICoreClientAPI capi, IWorldMapManager mapSink) : base(capi, mapSink)
    {
        this.capi = capi;
        pinTex = new LoadedTexture(capi);
    }

    public override void Render(GuiElementMap mapElem, float dt)
    {
        var positions = HidePlayerInfoModSystem.Instance?.partyPositions;
        if (!Active || positions == null || positions.Length == 0) return;

        int w = Math.Max(1, (int)mapElem.Bounds.InnerWidth);
        int h = Math.Max(1, (int)mapElem.Bounds.InnerHeight);

        using (var surface = new ImageSurface(Format.Argb32, w, h))
        using (var ctx = new Context(surface))
        {
            ctx.Operator = Operator.Source;
            ctx.SetSourceRGBA(0, 0, 0, 0);
            ctx.Paint();
            ctx.Operator = Operator.Over;

            double pinRadius = GuiElement.scaled(4);
            double fontSize = GuiElement.scaled(11);
            ctx.SelectFontFace("sans-serif", FontSlant.Normal, FontWeight.Bold);
            ctx.SetFontSize(fontSize);

            foreach (var pos in positions)
            {
                var wpos = new Vec3d(pos.X, pos.Y, pos.Z);
                var view = new Vec2f();
                mapElem.TranslateWorldPosToViewPos(wpos, ref view);

                double px = view.X;
                double py = view.Y;

                // Skip if off-screen
                if (px < -pinRadius || px > w + pinRadius || py < -pinRadius || py > h + pinRadius)
                    continue;

                // Filled circle pin
                ctx.SetSourceRGBA(0.2, 0.6, 1.0, 0.9);
                ctx.Arc(px, py, pinRadius, 0, 2 * Math.PI);
                ctx.Fill();

                // Border
                ctx.SetSourceRGBA(1, 1, 1, 0.8);
                ctx.Arc(px, py, pinRadius, 0, 2 * Math.PI);
                ctx.LineWidth = GuiElement.scaled(1);
                ctx.Stroke();

                // Name label
                if (!string.IsNullOrEmpty(pos.PlayerName))
                {
                    double labelX = px + pinRadius + GuiElement.scaled(3);
                    double labelY = py + fontSize * 0.35;

                    // Text shadow
                    ctx.SetSourceRGBA(0, 0, 0, 0.7);
                    ctx.MoveTo(labelX + 1, labelY + 1);
                    ctx.ShowText(pos.PlayerName);

                    // Text
                    ctx.SetSourceRGBA(1, 1, 1, 1);
                    ctx.MoveTo(labelX, labelY);
                    ctx.ShowText(pos.PlayerName);
                }
            }

            capi.Gui.LoadOrUpdateCairoTexture(surface, false, ref pinTex);
        }

        capi.Render.Render2DTexture(
            pinTex.TextureId,
            (float)mapElem.Bounds.renderX,
            (float)mapElem.Bounds.renderY,
            (float)mapElem.Bounds.InnerWidth,
            (float)mapElem.Bounds.InnerHeight,
            50f,
            new Vec4f(1, 1, 1, 1)
        );
    }

    public override void OnMapClosedClient()
    {
        pinTex?.Dispose();
        pinTex = new LoadedTexture(capi);
    }

    public override void Dispose()
    {
        pinTex?.Dispose();
        pinTex = null;
    }
}
