using System;
using System.Linq;
using Lumina.Excel.GeneratedSheets;
using XivCommon.Functions.ContextMenu;

namespace FFLogsViewer
{
    internal class ContextMenu : IDisposable
    {
        internal ContextMenu(Plugin plugin)
        {
            this.Plugin = plugin;

            this.Plugin.Common.Functions.ContextMenu.OpenContextMenu += OnOpenContextMenu;
        }

        private Plugin Plugin { get; }

        public void Dispose()
        {
            this.Plugin.Common.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
        }

        private void OnOpenContextMenu(ContextMenuOpenArgs args)
        {
            if (!IsMenuValid(args))
                return;

            if (this.Plugin.Configuration.ContextMenuStreamer)
            {
                if (!this.Plugin._ui.Visible)
                    return;

                this.SearchPlayerFromMenu(args);
            }
            else
            {
                args.Items.Add(new NormalContextMenuItem(this.Plugin.Configuration.ContextMenuButtonName, Search));
            }
        }

        private void Search(ContextMenuItemSelectedArgs args)
        {
            if (!IsMenuValid(args))
                    return;

            this.SearchPlayerFromMenu(args);
        }

        private static bool IsMenuValid(BaseContextMenuArgs args)
        {
            switch (args.ParentAddonName)
            {
                case null: // Nameplate/Model menu
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                case "_PartyList":
                case "LinkShell":
                case "CrossWorldLinkshell":
                case "ContentMemberList": // Eureka/Bozja/...
                    return args.Text != null && args.ActorWorld != 0 && args.ActorWorld != 65535;

                default:
                    return false;
            }
        }

        private void SearchPlayerFromMenu(BaseContextMenuArgs args)
        {
            var world = this.Plugin.Pi.Data.GetExcelSheet<World>()
                .FirstOrDefault(x => x.RowId == args.ActorWorld);

            if (world == null)
                return;

            var playerName = $"{args.Text}@{world.Name}";

            this.Plugin.SearchPlayer(playerName);
        }
    }
}