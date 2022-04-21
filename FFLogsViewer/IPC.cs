using Dalamud.Plugin.Ipc;

namespace FFLogsViewer;

public class IPC
{
    // Penumbra support
    public static bool PenumbraEnabled { get; private set; }
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
        penumbraApiVersionSubscriber = Service.Interface.GetIpcSubscriber<int>("Penumbra.ApiVersion");

        if (PenumbraApiVersion == 3)
        {
            penumbraResolveDefaultSubscriber = Service.Interface.GetIpcSubscriber<string, string>("Penumbra.ResolveDefaultPath");
            PenumbraEnabled = true;
        }
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
}
