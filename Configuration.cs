using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FFLogsViewer
{
    [Serializable]
    internal class Configuration : IPluginConfiguration
    {
        [NonSerialized] public DalamudPluginInterface PluginInterface;
        public string ClientId { get; set; } = "91907adb-5234-4e8d-bb78-7010587b4e87";

        public string ClientSecret { get; set; } = "TllDOR1ra0bXndHVWBJaShElu9DIgD3OcLkhtEjC";

        public bool ContextMenu { get; set; } = true;
        //internal string ButtonName { get; set; } = "Search FF Logs"; // TODO ContextMenu

        public int Version { get; set; } = 0;

        internal void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        internal void Save()
        {
            this.PluginInterface.SavePluginConfig(this);
        }
    }
}