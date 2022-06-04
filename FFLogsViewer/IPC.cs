using Dalamud.Plugin.Ipc;

namespace FFLogsViewer;

public class IPC
{
    // Penumbra support
    public static bool IsPenumbraIpcEnabled { get; private set; }

    private const int PenumbraSupportedApiVersion = 4;

    private static ICallGateSubscriber<int> penumbraInitializedSubscriber = null!;
    private static ICallGateSubscriber<int> penumbraDisposedSubscriber = null!;
    private static ICallGateSubscriber<int> penumbraApiVersionSubscriber = null!;
    private static ICallGateSubscriber<string, string> penumbraResolveDefaultSubscriber = null!;
    public static int PenumbraApiVersion
    {
        get
        {
            try
            {
                return penumbraApiVersionSubscriber.InvokeFunc();
            }
            catch
            {
                return 0;
            }
        }
    }

    public static void Initialize()
    {
        penumbraInitializedSubscriber = Service.Interface.GetIpcSubscriber<int>("Penumbra.Initialized");
        penumbraInitializedSubscriber.Subscribe(UpdatePenumbraStatus);
        penumbraDisposedSubscriber = Service.Interface.GetIpcSubscriber<int>("Penumbra.Disposed");
        penumbraDisposedSubscriber.Subscribe(UpdatePenumbraStatus);
        penumbraApiVersionSubscriber = Service.Interface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
        penumbraResolveDefaultSubscriber = Service.Interface.GetIpcSubscriber<string, string>("Penumbra.ResolveDefaultPath");
        UpdatePenumbraStatus();
    }

    public static void Dispose()
    {
        penumbraInitializedSubscriber.Unsubscribe(UpdatePenumbraStatus);
        penumbraDisposedSubscriber.Unsubscribe(UpdatePenumbraStatus);
    }

    public static string ResolvePenumbraPath(string path)
    {
        try
        {
            return penumbraResolveDefaultSubscriber.InvokeFunc(path);
        }
        catch
        {
            return path;
        }
    }

    private static void UpdatePenumbraStatus()
    {
        IsPenumbraIpcEnabled = PenumbraApiVersion == PenumbraSupportedApiVersion;
    }
}
