using Barotrauma.Lights;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

namespace NoLightNoName;

[HarmonyPatch]
public class LightMapSampler
{
    private static RenderTarget2D? _sampledTarget;
    private static Color[] _pixels = [];
    private static int _sampledWidth, _sampledHeight;
    private static bool _valid;
    private static double _snapshotTimer = 0.0;

    public static RenderTarget2D? SampledTarget => _sampledTarget;

    private static ConditionalWeakTable<Character, object> _isNameShown = new();
    public static ConditionalWeakTable<Character, object> IsNameShown => _isNameShown;

    public static Color GetColor(Vector2 worldPosition, int sampleRange = 0)
    {
        if (!_valid || GameMain.GameScreen?.Cam is not { } cam) { return Color.Black; }

        float scale = GameSettings.CurrentConfig.Graphics.LightMapScale;
        Vector2 screen = cam.WorldToScreen(worldPosition);
        int cx = MathHelper.Clamp((int)(screen.X * scale * Plugin.SampleResolution), 0, _sampledWidth - 1);
        int cy = MathHelper.Clamp((int)(screen.Y * scale * Plugin.SampleResolution), 0, _sampledHeight - 1);

        if (sampleRange <= 0) { return _pixels[cy * _sampledWidth + cx]; }

        int x0 = Math.Max(0, cx - sampleRange), x1 = Math.Min(_sampledWidth - 1, cx + sampleRange);
        int y0 = Math.Max(0, cy - sampleRange), y1 = Math.Min(_sampledHeight - 1, cy + sampleRange);

        int r = 0, g = 0, b = 0, count = 0;
        for (int row = y0; row <= y1; row++)
        {
            int rowOffset = row * _sampledWidth;
            for (int col = x0; col <= x1; col++)
            {
                Color p = _pixels[rowOffset + col];
                r += p.R; g += p.G; b += p.B;
                count++;
            }
        }

        return count > 0
            ? new((byte)(r / count), (byte)(g / count), (byte)(b / count))
            : Color.Black;
    }

    [HarmonyPatch(typeof(LightManager), nameof(LightManager.RenderLightMap)), HarmonyPostfix]
    public static void LightManager_RenderLightMap_Postfix(LightManager __instance, GraphicsDevice graphics, SpriteBatch spriteBatch)
    {
        if (__instance.LightMap is not RenderTarget2D lightMap
            || lightMap.IsContentLost
            || !GameMain.WindowActive
            || Character.Controlled is not Character { IsHuman: true } controlled)
        {
            _valid = false;
            return;
        }

        if (Timing.TotalTime - _snapshotTimer < 0.5) { return; }

        try
        {
            int targetW = Math.Max(1, (int)(lightMap.Width * Plugin.SampleResolution));
            int targetH = Math.Max(1, (int)(lightMap.Height * Plugin.SampleResolution));

            if (_sampledTarget == null
                || _sampledTarget.GraphicsDevice != graphics
                || _sampledWidth != targetW
                || _sampledHeight != targetH)
            {
                _sampledTarget?.Dispose();
                _sampledTarget = new RenderTarget2D(graphics, targetW, targetH, false, lightMap.Format, DepthFormat.None);
                _sampledWidth = targetW;
                _sampledHeight = targetH;
                int size = _sampledWidth * _sampledHeight;
                if (_pixels.Length != size)
                {
                    Array.Resize(ref _pixels, size);
                }
            }

            graphics.SetRenderTarget(_sampledTarget);
            graphics.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
            spriteBatch.Draw(lightMap, new Rectangle(0, 0, targetW, targetH), Color.White);
            spriteBatch.End();
            graphics.SetRenderTarget(null);

            _sampledTarget.GetData(_pixels);

            _isNameShown.Clear();
            foreach (var character in Character.CharacterList)
            {
                if (character.IsVisible
                    && character != Character.Controlled
                    && character.Info is not null
                    && character.hudInfoVisible
                    && controlled.FocusedCharacter != character
                    && character.AnimController?.GetLimb(LimbType.Head) is Limb head)
                {
                    Color sampledResult = GetColor(head.body.DrawPosition);
                    float luminance = sampledResult.GetRelativeLuminance();
                    if (luminance > 0.0221244f)
                    {
                        _isNameShown.Add(character, null!);
                    }
                }
            }
            _valid = true;
        }
        catch
        {
            _valid = false;
        }

        _snapshotTimer = Timing.TotalTime;
    }
}
