using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using WorldSwitcher.Windows;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WorldSwitcher;

public static class Utils
{
    public static unsafe AtkUnitBase* Base(this AddonArgs args) => (AtkUnitBase*)args.Addon;
}

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    private const string CommandName = "/wswitch";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("WorldSwitcher");
    private ConfigWindow ConfigWindow { get; init; }

    private IChatGui Chat { get; init; }
    private IGameGui GameGui { get; init; }
    private IAddonLifecycle AddonLifecycle { get; init; }

    internal string[] LastSeenListEntries { get; set; } = [];
    internal string LastSeenCurrentWorld { get; set; } = "";
    internal unsafe AtkUnitBase* LastSeenSwitcher;
    
    private AtkValueArray FormatValues(params object[] values)
    {
        return new AtkValueArray(values);
    }

    public Plugin(IChatGui chat, IGameGui gameGui, IAddonLifecycle addonLifecycle)
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open config"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        Chat = chat;
        Chat.ChatMessage += OnChatMessage;
        
        AddonLifecycle = addonLifecycle;
        AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "WorldTravelSelect", SetListEntries);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "WorldTravelSelect", Cleanup);

        GameGui = gameGui;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleConfigUI();
    }

    private unsafe void Cleanup(AddonEvent eventType, AddonArgs addonInfo)
    {
        LastSeenSwitcher = null;
    }

    private unsafe void SetListEntries(AddonEvent eventType, AddonArgs addonInfo)
    {
        try
        {
            var addon = (AtkUnitBase*)addonInfo.Addon;
            LastSeenSwitcher = addon;
            
            var rootNode = addon->RootNode;
            var windowComponent = (AtkComponentNode*) rootNode->ChildNode;
            var informationBox = windowComponent->PrevSiblingNode;
            var informationBoxBorder = informationBox->PrevSiblingNode;
            var worldListComponent = (AtkComponentNode*)informationBoxBorder->PrevSiblingNode;
            var nodeList = (AtkComponentNode**)worldListComponent->Component->UldManager.NodeList;
            
            
            LastSeenListEntries = new string[18];

            for (var i = 0; i < 18; i++) {
                var node = nodeList[i + 3];
                LastSeenListEntries[i] = "";
                if (node->AtkResNode.Y == 0) continue;
                var nameNode = (AtkTextNode*) node->Component->UldManager.NodeList[4];
                LastSeenListEntries[i] = nameNode->NodeText.ToString();
            }
            
            var worldListHeader = (AtkTextNode*) worldListComponent->AtkResNode.PrevSiblingNode;
            var currentWorldContainer = worldListHeader->AtkResNode.PrevSiblingNode;
            var currentWorldName = (AtkTextNode*) currentWorldContainer->ChildNode;
            LastSeenCurrentWorld = currentWorldName->NodeText.ToString();
        }
        catch (Exception e)
        {
        }
    }

    private unsafe void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (sender.ToString() != "Sonar")
        {
            return;
        }
        
        string smessage = message.ToString();
        
        Regex rp = new Regex("was just killed", RegexOptions.IgnoreCase);
        if (rp.IsMatch(smessage))
        {
            return;
        }
        
        var ranks = Configuration.SRanks ? "S|SS|SSMinion" : "";
        ranks += Configuration.ARanks ? "|A" : "";
        ranks += Configuration.BRanks ? "|B" : "";
        var rankPattern = "Rank (" + ranks.TrimStart('|') + ")";
        Regex rr = new Regex(rankPattern, RegexOptions.IgnoreCase);
        if (!rr.IsMatch(smessage))
        {
            return;
        }

        if (Configuration.OpenMapLink)
        {
            foreach (var payload in message.Payloads)
            {
                if (payload is MapLinkPayload mapLinkPayload)
                {
                    GameGui.OpenMapWithMapLink(mapLinkPayload);
                    break;
                }
            }
        }

        if (LastSeenSwitcher == null)
        {
            return;
        }
        
        Regex r = new Regex(@"\<[^A-Z]*([^\>]+)\>", RegexOptions.IgnoreCase);
        Match m = r.Match(smessage);
        if (m.Groups.Count < 2)
        {
            return;
        }

        var world = m.Groups.Values.ToArray()[1].ToString();

        if (Configuration.CloseOnCurrent && LastSeenCurrentWorld != "" && LastSeenCurrentWorld == world)
        {
            try
            {
                var values = FormatValues(0);
                LastSeenSwitcher->FireCallback((uint)values.Length, values);
            }
            catch (Exception e)
            {}
            return;
        }

        if (!Configuration.WorldSwitchEnabled)
        {
            return;
        }

        var idx = -1;
        for (var i = 0; i < 18; i++)
        {
            if (world == LastSeenListEntries[i])
            {
                idx = i;
                break;
            }
        }

        if (idx == -1)
        {
            return;
        }
        
        try
        {
            var values = FormatValues(idx + 2);
            LastSeenSwitcher->FireCallback((uint)values.Length, values);
        }
        catch (Exception e)
        {
        }
    }

    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
