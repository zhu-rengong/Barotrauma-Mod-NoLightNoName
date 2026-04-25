using Barotrauma.LuaCs.Data;

namespace NoLightNoName;

public partial class Plugin
{
    private static ISettingBase<bool> _samplePixelAtCursorSetting = null!;
    public static bool SamplePixelAtCursor
    {
        get => _samplePixelAtCursorSetting.Value;
        set => _samplePixelAtCursorSetting.TrySetValue(value);
    }

    private partial void LoadConfigProjSpecific()
    {
        TryGetConfig("SamplePixelAtCursor", out _samplePixelAtCursorSetting);
    }
}