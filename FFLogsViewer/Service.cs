using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFLogsViewer.GUI.Config;
using FFLogsViewer.GUI.Main;
using FFLogsViewer.Manager;
using XivCommon;

namespace FFLogsViewer;

internal class Service
{
    internal static Configuration Configuration { get; set; } = null!;
    internal static Commands Commands { get; set; } = null!;
    internal static ConfigWindow ConfigWindow { get; set; } = null!;
    internal static MainWindow MainWindow { get; set; } = null!;
    internal static GameDataManager GameDataManager { get; set; } = null!;
    internal static CharDataManager CharDataManager { get; set; } = null!;
    internal static PartyListManager PartyListManager { get; set; } = null!;
    internal static FFLogsClient FfLogsClient { get; set; } = null!;
    internal static XivCommonBase Common { get; set; } = null!;

    [PluginService]
    internal static DalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService]
    internal static ChatGui ChatGui { get; private set; } = null!;
    [PluginService]
    internal static ClientState ClientState { get; private set; } = null!;
    [PluginService]
    internal static CommandManager CommandManager { get; private set; } = null!;
    [PluginService]
    internal static Condition Condition { get; private set; } = null!;
    [PluginService]
    internal static DataManager DataManager { get; private set; } = null!;
    [PluginService]
    internal static FlyTextGui FlyTextGui { get; private set; } = null!;
    [PluginService]
    internal static SigScanner SigScanner { get; private set; } = null!;
    [PluginService]
    internal static TargetManager TargetManager { get; private set; } = null!;
}
