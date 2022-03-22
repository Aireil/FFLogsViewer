using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FFLogsViewer
{
    [Serializable]
    internal class Configuration : IPluginConfiguration
    {
        [NonSerialized] public DalamudPluginInterface PluginInterface;
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public bool ContextMenu { get; set; } = true;

        public bool ContextMenuStreamer { get; set; } = false;

        public bool OpenInBrowser { get; set; } = false;

        public string ContextMenuButtonName { get; set; } = "Search FF Logs";

        public bool ShowSpoilers { get; set; } = false;

        public bool DisplayOldRaid { get; set; } = false;

        public bool DisplayOldUltimate { get; set; } = false;

        public bool HideInCombat { get; set; } = false;

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
