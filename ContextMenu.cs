using System;
using System.Linq;
using Lumina.Excel.GeneratedSheets;
using XivCommon.Functions.ContextMenu;

namespace FFLogsViewer
{
    public class ContextMenu : IDisposable
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

            args.Items.Add(new NormalContextMenuItem("Search on FF Logs", Search));
        }

        private void Search(ContextMenuItemSelectedArgs args)
        {
            if (!IsMenuValid(args))
                return;

            var world = this.Plugin.Pi.Data.GetExcelSheet<World>()
                .FirstOrDefault(x => x.RowId == args.ActorWorld);

            if (world == null)
                return;

            var playerName = $"{args.Text}@{world.Name}";

            this.Plugin.SearchPlayer(playerName);
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
                    return args.Text != null && args.ActorWorld != 0 && args.ActorWorld != 65535;

                default:
                    return false;
            }
        }
    }
}