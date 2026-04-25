using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Utilities;

namespace NoLightNoName;

[HarmonyPatch]
public class Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.Update)), HarmonyPostfix]
    public static void Character_Update_Postfix(Character __instance)
    {
        if (__instance != Character.Controlled
            && __instance.IsHuman
            && __instance.hudInfoVisible
            && (Character.Controlled is not Character controlled || controlled.FocusedCharacter != __instance)
            && __instance.AnimController?.GetLimb(LimbType.Head) is Limb head)
        {
            Color sampledResult = LightMapSampler.GetColor(head.body.DrawPosition, 4);
            float luminance = sampledResult.GetRelativeLuminance();
            bool isLight = luminance > 0.0358f;
            if (!isLight)
            {
                __instance.hudInfoVisible = false;
            }
        }
    }

    [HarmonyPatch(typeof(GUI), nameof(GUI.Draw)), HarmonyPostfix]
    public static void GUI_Draw_Postfix(Camera cam, SpriteBatch spriteBatch)
    {
        if (!Plugin.SamplePixelAtCursor || Screen.Selected != GameMain.GameScreen) { return; }
        Vector2 mousePos = PlayerInput.MousePosition;
        Vector2 cursorPosition = cam.ScreenToWorld(PlayerInput.MousePosition);
        Vector2 textPos = mousePos + new Vector2(20f, 10f);
        Color sampledResult = LightMapSampler.GetColor(cursorPosition, 4);
        float luminance = sampledResult.GetRelativeLuminance();

        GUI.DrawString(
            spriteBatch,
            textPos,
            $"RGB: {sampledResult.R},{sampledResult.G},{sampledResult.B} | L: {luminance}",
            Color.White,
            Color.Black * 0.7f,
            backgroundPadding: 2,
            font: GUIStyle.SmallFont
        );
    }
}
