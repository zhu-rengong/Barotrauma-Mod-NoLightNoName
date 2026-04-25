using Barotrauma.LuaCs.Data;
using System.Diagnostics.CodeAnalysis;

namespace NoLightNoName;

public partial class Plugin
{
    private void LoadConfig()
    {
        LoadConfigProjSpecific();
    }

    private partial void LoadConfigProjSpecific();

    private bool TryGetConfig<T>(string name, [NotNullWhen(true)] out T setting) where T : ISettingBase
    {
        if (!ConfigService.TryGetConfig(_package, name, out setting))
        {
            LoggerService.LogError($"Failed to find config named {name}!");
            return false;
        }

        return true;
    }
}