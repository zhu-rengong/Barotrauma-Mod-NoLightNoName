using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;

namespace NoLightNoName;

[HarmonyPatch]
class Patches
{
    static bool _isNameHidden;

    [HarmonyPatch(typeof(Character), nameof(Character.DrawFront)), HarmonyPrefix]
    static void Character_DrawFront_Prefix(Character __instance)
    {
        if (Character.Controlled is Character { IsHuman: true } controlled
            && __instance != Character.Controlled
            && __instance.Info is not null
            && __instance.hudInfoVisible
            && controlled.FocusedCharacter != __instance
            && __instance.AnimController?.GetLimb(LimbType.Head) is Limb head)
        {
            Color sampledResult = LightMapSampler.GetColor(head.body.DrawPosition, 4);
            float luminance = sampledResult.GetRelativeLuminance();
            _isNameHidden = luminance < 0.0221244f;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.DrawFront)), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> Character_DrawFront_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        /* version: 1.12.7.0 | commit: 8d90ccb4a30af3b43ec9ab58e7bbb45d56ae1267
        604	0794	ldsfld	class Barotrauma.GUIFont Barotrauma.GUIStyle::Font
        605	0799	ldarg.1
        606	079A	ldloc.s	name (19)
        607	079C	ldloc.s	namePos (21)
        608	079E	ldc.r4	1
        609	07A3	ldarg.2
        610	07A4	callvirt	instance float32 Barotrauma.Camera::get_Zoom()
        611	07A9	div
        612	07AA	ldc.r4	1
        613	07AF	ldarg.2
        614	07B0	callvirt	instance float32 Barotrauma.Camera::get_Zoom()
        615	07B5	div
        616	07B6	newobj	instance void [XNATypes]Microsoft.Xna.Framework.Vector2::.ctor(float32, float32)
        617	07BB	call	valuetype [XNATypes]Microsoft.Xna.Framework.Vector2 [XNATypes]Microsoft.Xna.Framework.Vector2::op_Addition(valuetype [XNATypes]Microsoft.Xna.Framework.Vector2, valuetype [XNATypes]Microsoft.Xna.Framework.Vector2)
        618	07C0	call	valuetype [XNATypes]Microsoft.Xna.Framework.Color [XNATypes]Microsoft.Xna.Framework.Color::get_Black()
        619	07C5	ldc.r4	0
        620	07CA	call	valuetype [XNATypes]Microsoft.Xna.Framework.Vector2 [XNATypes]Microsoft.Xna.Framework.Vector2::get_Zero()
        621	07CF	ldc.r4	1
        622	07D4	ldarg.2
        623	07D5	callvirt	instance float32 Barotrauma.Camera::get_Zoom()
        624	07DA	div
        625	07DB	ldc.i4.0
        626	07DC	ldc.r4	0.001
        627	07E1	ldc.i4.s	18
        628	07E3	callvirt	instance void Barotrauma.GUIFont::DrawString(class [MonoGame.Framework.Windows.NetStandard]Microsoft.Xna.Framework.Graphics.SpriteBatch, class Barotrauma.LocalizedString, valuetype [XNATypes]Microsoft.Xna.Framework.Vector2, valuetype [XNATypes]Microsoft.Xna.Framework.Color, float32, valuetype [XNATypes]Microsoft.Xna.Framework.Vector2, float32, valuetype [MonoGame.Framework.Windows.NetStandard]Microsoft.Xna.Framework.Graphics.SpriteEffects, float32, valuetype [BarotraumaCore]Barotrauma.Alignment)
        */
        MethodInfo DrawStringHijacked = AccessTools.Method(
        typeof(GUIFont), nameof(GUIFont.DrawString),
        [
            typeof(SpriteBatch),
            typeof(LocalizedString),
            typeof(Vector2),
            typeof(Color),
            typeof(float),
            typeof(Vector2),
            typeof(float),
            typeof(SpriteEffects),
            typeof(float),
            typeof(Alignment)
        ]);

        try
        {
            return new CodeMatcher(instructions)
                .MatchForward(true,
                    new(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.Black))),
                    new(OpCodes.Ldc_R4, 0.0f))
                .MatchForward(true,
                    [new(OpCodes.Ldc_R4, 0.001f)])
                .SearchForward(i => i.Calls(DrawStringHijacked))
                .ThrowIfInvalid($"Failed to find the 1st target method call for hijacking. (draw drop shadow)")
                .Set(OpCodes.Call, AccessTools.Method(typeof(Patches), nameof(DrawStringHijacker)))
                .SearchForward(i => i.Calls(DrawStringHijacked))
                .ThrowIfInvalid($"Failed to find the 2nd target method call for hijacking. (draw colored name)")
                .Set(OpCodes.Call, AccessTools.Method(typeof(Patches), nameof(DrawStringHijacker)))
                .InstructionEnumeration();
        }
        catch (InvalidOperationException ex)
        {
            Plugin.LoggerService.LogError(ex.Message);
        }

        return instructions;
    }

    static void DrawStringHijacker(GUIFont font, SpriteBatch sb, LocalizedString text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects spriteEffects, float layerDepth, Alignment alignment)
    {
        if (!_isNameHidden)
        {
            font.DrawString(sb, text, position, color, rotation, origin, scale, spriteEffects, layerDepth, alignment);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.DrawFront)), HarmonyPostfix]
    static void Character_DrawFront_Postfix()
    {
        _isNameHidden = false;
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

