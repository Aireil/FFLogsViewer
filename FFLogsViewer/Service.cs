using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFLogsViewer.GUI.Config;
using FFLogsViewer.GUI.Main;
using FFLogsViewer.Manager;

namespace FFLogsViewer;

internal class Service
{
    internal static Configuration Configuration { get; set; } = null!;
    internal static Commands Commands { get; set; } = null!;
    internal static ConfigWindow ConfigWindow { get; set; } = null!;
    internal static MainWindow MainWindow { get; set; } = null!;
    internal static CharDataManager CharDataManager { get; set; } = null!;
    internal static GameDataManager GameDataManager { get; set; } = null!;
    internal static HistoryManager HistoryManager { get; set; } = null!;
    internal static OpenWithManager OpenWithManager { get; set; } = null!;
    internal static TeamManager TeamManager { get; set; } = null!;
    internal static FFLogsClient FFLogsClient { get; set; } = null!;
    internal static DalamudContextMenu ContextMenu { get; set; } = null!;

    [PluginService] internal static DalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFlyTextGui FlyTextGui { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
}
