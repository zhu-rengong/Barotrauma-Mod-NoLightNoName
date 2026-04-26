using Barotrauma.Lights;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

namespace NoLightNoName;

[HarmonyPatch]
public class LightMapSampler
{
    private static Color[] _pixels = [];
    private static int _width, _height;
    private static bool _valid;
    private static double _snapshotTimer = 0.0;

    public static Color GetColor(Vector2 worldPosition, int sampleRange = 0)
    {
        if (!_valid || GameMain.GameScreen?.Cam is not { } cam) { return Color.Black; }

        float scale = GameSettings.CurrentConfig.Graphics.LightMapScale;
        Vector2 screen = cam.WorldToScreen(worldPosition);
        int cx = MathHelper.Clamp((int)(screen.X * scale), 0, _width - 1);
        int cy = MathHelper.Clamp((int)(screen.Y * scale), 0, _height - 1);

        if (sampleRange <= 0) { return _pixels[cy * _width + cx]; }

        int x0 = Math.Max(0, cx - sampleRange), x1 = Math.Min(_width - 1, cx + sampleRange);
        int y0 = Math.Max(0, cy - sampleRange), y1 = Math.Min(_height - 1, cy + sampleRange);

        int r = 0, g = 0, b = 0, count = 0;
        for (int row = y0; row <= y1; row++)
        {
            int rowOffset = row * _width;
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
    public static void LightManager_RenderLightMap_Postfix(LightManager __instance)
    {
        if (__instance.LightMap is not RenderTarget2D lightMap
            || lightMap.IsContentLost
            || !GameMain.WindowActive
            || Character.Controlled is not Character { IsHuman: true })
        {
            _valid = false;
            return;
        }

        if (Timing.TotalTime - _snapshotTimer < 0.5) { return; }

        try
        {
            if (_width != lightMap.Width || _height != lightMap.Height)
            {
                _width = lightMap.Width;
                _height = lightMap.Height;
                int size = _width * _height;
                if (_pixels.Length != size)
                {
                    Array.Resize(ref _pixels, size);
                }
            }
            lightMap.GetData(_pixels);
            _valid = true;
        }
        catch
        {
            _valid = false;
        }

        _snapshotTimer = Timing.TotalTime;
    }
}
