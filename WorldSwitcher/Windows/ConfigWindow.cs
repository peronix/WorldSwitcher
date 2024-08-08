using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace WorldSwitcher.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;
    public ConfigWindow(Plugin plugin) : base("World Switcher Config###WorldSwitcherConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(250, 180);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var isEnabled = Configuration.WorldSwitchEnabled;
        if (ImGui.Checkbox("World Switcher Enabled", ref isEnabled))
        {
            Configuration.WorldSwitchEnabled = isEnabled;
            Configuration.Save();
        }
        
        var closeOnCurrent = Configuration.CloseOnCurrent;
        if (ImGui.Checkbox("Current World -> Close Switcher", ref closeOnCurrent))
        {
            Configuration.CloseOnCurrent = closeOnCurrent;
            Configuration.Save();
        }
        
        var sRanks = Configuration.SRanks;
        if (ImGui.Checkbox("S Ranks", ref sRanks))
        {
            Configuration.SRanks = sRanks;
            Configuration.Save();
        }
        
        var aRanks = Configuration.ARanks;
        if (ImGui.Checkbox("A Ranks", ref aRanks))
        {
            Configuration.ARanks = aRanks;
            Configuration.Save();
        }
        
        var bRanks = Configuration.BRanks;
        if (ImGui.Checkbox("B Ranks", ref bRanks))
        {
            Configuration.BRanks = bRanks;
            Configuration.Save();
        }
    }
}
