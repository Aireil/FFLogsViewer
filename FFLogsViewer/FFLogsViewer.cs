using Dalamud.Interface.Windowing;
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
    private readonly FFLogsViewerProvider ffLogsViewerProvider;

    public FFLogsViewer(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Service.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Configuration.Initialize();

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

        ContextMenu.Enable();

        this.ffLogsViewerProvider = new FFLogsViewerProvider(pluginInterface, new FFLogsViewerAPI());

        Service.Interface.UiBuilder.OpenMainUi += OpenMainUi;
        Service.Interface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Service.Interface.UiBuilder.Draw += this.windowSystem.Draw;
    }

    public void Dispose()
    {
        this.ffLogsViewerProvider.Dispose();
        Commands.Dispose();
        Service.OpenWithManager.Dispose();

        ContextMenu.Disable();

        Service.Interface.UiBuilder.OpenMainUi -= OpenMainUi;
        Service.Interface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Service.Interface.UiBuilder.Draw -= this.windowSystem.Draw;
    }

    private static void OpenMainUi()
    {
        Service.MainWindow.Open();
    }

    private static void OpenConfigUi()
    {
        Service.ConfigWindow.IsOpen = true;
    }
}
