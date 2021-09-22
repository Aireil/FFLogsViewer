using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace FFLogsViewer
{
	internal class DalamudApi
	{
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static BuddyList BuddyList { get; private set; }
        [PluginService] public static ChatGui ChatGui { get; private set; }
        [PluginService] public static ChatHandlers ChatHandlers { get; private set; }
        [PluginService] public static ClientState ClientState { get; private set; }
        [PluginService] public static CommandManager CommandManager { get; private set; }
        [PluginService] public static Condition Condition { get; private set; }
        [PluginService] public static DataManager DataManager { get; private set; }
        [PluginService] public static FateTable FateTable { get; private set; }
        [PluginService] public static FlyTextGui FlyTextGui { get; private set; }
        [PluginService] public static Framework Framework { get; private set; }
        [PluginService] public static GameGui GameGui { get; private set; }
        [PluginService] public static GameNetwork GameNetwork { get; private set; }
        [PluginService] public static JobGauges JobGauges { get; private set; }
        [PluginService] public static KeyState KeyState { get; private set; }
        [PluginService] public static LibcFunction LibcFunction { get; private set; }
        [PluginService] public static ObjectTable ObjectTable { get; private set; }
        [PluginService] public static PartyFinderGui PartyFinderGui { get; private set; }
        [PluginService] public static PartyList PartyList { get; private set; }
        [PluginService] public static SigScanner SigScanner { get; private set; }
        [PluginService] public static TargetManager TargetManager { get; private set; }
        [PluginService] public static ToastGui ToastGui { get; private set; }
	}
}
