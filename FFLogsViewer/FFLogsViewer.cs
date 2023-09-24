using Dalamud.ContextMenu;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFLogsViewer.API;
using FFLogsViewer.GUI.Config;
using FFLogsViewer.GUI.Main;
using FFLogsViewer.Manager;

namespace FFLogsViewer;

// ReSharper disable once UnusedType.Global
public sealed class FFLogsViewer : IDalamudPlugin
{
    private readonly WindowSystem windowSystem;
    private readonly ContextMenu contextMenu;
    private readonly FFLogsViewerProvider ffLogsViewerProvider;

    public FFLogsViewer(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Service.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Configuration.Initialize();

        IPC.Initialize();

        Service.Commands = new Commands();
        Service.CharDataManager = new CharDataManager();
        Service.GameDataManager = new GameDataManager();
        Service.OpenWithManager = new OpenWithManager();
        Service.HistoryManager = new HistoryManager();
        Service.TeamManager = new TeamManager();
        Service.FFLogsClient = new FFLogsClient();

        Service.MainWindow = new MainWindow();
        Service.ConfigWindow = new ConfigWindow();
        this.windowSystem = new WindowSystem("FFLogsViewer");
        this.windowSystem.AddWindow(Service.ConfigWindow);
        this.windowSystem.AddWindow(Service.MainWindow);

        Service.ContextMenu = new DalamudContextMenu(pluginInterface);
        this.contextMenu = new ContextMenu();

        this.ffLogsViewerProvider = new FFLogsViewerProvider(pluginInterface, new FFLogsViewerAPI());

        Service.Interface.UiBuilder.OpenMainUi += OpenMainUi;
        Service.Interface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Service.Interface.UiBuilder.Draw += this.windowSystem.Draw;
    }

    public void Dispose()
    {
        this.ffLogsViewerProvider.Dispose();
        IPC.Dispose();
        Commands.Dispose();
        Service.ContextMenu.Dispose();
        this.contextMenu.Dispose();
        Service.OpenWithManager.Dispose();

        Service.Interface.UiBuilder.OpenMainUi -= OpenMainUi;
        Service.Interface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Service.Interface.UiBuilder.Draw -= this.windowSystem.Draw;
    }

    private static void OpenMainUi()
    {
        Service.MainWindow.IsOpen = true;
    }

    private static void OpenConfigUi()
    {
        Service.ConfigWindow.IsOpen = true;
    }
}
