using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FFLogsViewer
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public string ClientId = "91907adb-5234-4e8d-bb78-7010587b4e87";

        public string ClientSecret = "TllDOR1ra0bXndHVWBJaShElu9DIgD3OcLkhtEjC";
        [NonSerialized] public DalamudPluginInterface PluginInterface;

        public int Version { get; set; } = 0;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;
        }

        public void Save()
        {
            PluginInterface.SavePluginConfig(this);
        }
    }
}