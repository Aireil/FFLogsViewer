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
            Plugin = plugin;

            Plugin.Common.Functions.ContextMenu.OpenContextMenu += OnOpenContextMenu;
        }

        private Plugin Plugin { get; }

        public void Dispose()
        {
            Plugin.Common.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
        }

        private void OnOpenContextMenu(ContextMenuOpenArgs args)
        {
            // foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(args))
            // {
            //     var name=descriptor.Name;
            //     var value=descriptor.GetValue(args);
            //     PluginLog.Information("{0}={1}",name,value);
            // }

            if (!IsValid(args))
                return;

            args.Items.Add(new NormalContextMenuItem("Search on FF Logs", Search));
        }

        private void Search(ContextMenuItemSelectedArgs args)
        {
            if (!IsValid(args))
                return;

            var playerName = string.Empty;
            switch (args.ParentAddonName)
            {
                case null: // Nameplate/Model menu
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                    var world = Plugin.Pi.Data.GetExcelSheet<World>()
                        .FirstOrDefault(x => x.RowId == args.ActorWorld);

                    if (world == null)
                        return;

                    playerName = $"{args.Text}@{world.Name}";
                    break;
            }

            Plugin.SearchPlayer(playerName);
        }

        private static bool IsValid(BaseContextMenuArgs args)
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