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

    private static ISettingBase<float> _sampleResolutionSetting = null!;
    public static float SampleResolution
    {
        get => _sampleResolutionSetting.Value;
        set => _sampleResolutionSetting.TrySetValue(value);
    }

    private partial void LoadConfigProjSpecific()
    {
        TryGetConfig("SamplePixelAtCursor", out _samplePixelAtCursorSetting);
        TryGetConfig("SampleResolution", out _sampleResolutionSetting);
    }
}