using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace WorldSwitcher;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool WorldSwitchEnabled { get; set; } = true;
    public bool CloseOnCurrent { get; set; } = true;
    public bool SRanks { get; set; } = true;
    public bool ARanks { get; set; } = false;
    public bool BRanks { get; set; } = false;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
