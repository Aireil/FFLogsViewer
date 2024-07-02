using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace FFLogsViewer.API;

/// <summary>
/// IPC for FFLogsViewer plugin.
/// </summary>
public class FFLogsViewerProvider
{
    /// <summary>
    /// API Version label.
    /// </summary>
    public const string LabelProviderApiVersion = "FFLogsViewer.APIVersion";

    /// <summary>
    /// IsInitialized state label.
    /// </summary>
    public const string LabelProviderIsInitialized = "FFLogsViewer.IsInitialized";

    /// <summary>
    /// Fetch and open player profile label.
    /// </summary>
    public const string LabelProviderFetchCharacter = "FFLogsViewer.FetchCharacter";

    /// <summary>
    /// API.
    /// </summary>
    public readonly IFFLogsViewerAPI API;

    /// <summary>
    /// Provider API Version.
    /// </summary>
    public ICallGateProvider<int>? ProviderAPIVersion;

    /// <summary>
    /// Provider IsInitialized state.
    /// </summary>
    public ICallGateProvider<bool>? ProviderIsInitialized;

    /// <summary>
    /// Provider Fetch and open player profile.
    /// </summary>
    public ICallGateProvider<string, ushort, bool>? ProviderFetchCharacter;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFLogsViewerProvider"/> class.
    /// </summary>
    /// <param name="pluginInterface">plugin interface.</param>
    /// <param name="api">plugin api.</param>
    public FFLogsViewerProvider(IDalamudPluginInterface pluginInterface, IFFLogsViewerAPI api)
    {
        this.API = api;

        try
        {
            this.ProviderAPIVersion = pluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
            this.ProviderAPIVersion.RegisterFunc(() => api.APIVersion);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderApiVersion}:\n{ex}");
        }

        try
        {
            this.ProviderIsInitialized = pluginInterface.GetIpcProvider<bool>(LabelProviderIsInitialized);
            this.ProviderIsInitialized.RegisterFunc(() => api.IsInitialized);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderIsInitialized}:\n{ex}");
        }

        try
        {
            this.ProviderFetchCharacter = pluginInterface.GetIpcProvider<string, ushort, bool>(LabelProviderFetchCharacter);
            this.ProviderFetchCharacter.RegisterFunc(api.FetchCharacter);
        }
        catch (Exception e)
        {
            Service.PluginLog.Error($"Error registering IPC provider for {LabelProviderFetchCharacter}:\n{e}");
        }

        this.API.IsInitialized = true;
        this.ProviderIsInitialized?.SendMessage();
    }

    /// <summary>
    /// Dispose IPC.
    /// </summary>
    public void Dispose()
    {
        this.API.IsInitialized = false;
        this.ProviderIsInitialized?.SendMessage();
        this.ProviderAPIVersion?.UnregisterFunc();
        this.ProviderIsInitialized?.UnregisterFunc();
    }
}
