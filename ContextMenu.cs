using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Web.UI.WebControls;
using Dalamud.Game.Text;
using Dalamud.Plugin;
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

        [HandleProcessCorruptedStateExceptions]
        private void OnOpenContextMenu(ContextMenuOpenArgs args)
        {
            if (!this.Plugin._ui.Visible) // TODO ContextMenu
                return;

            try
            {
                if (!IsMenuValid(args))
                    return;

                var world = this.Plugin.Pi.Data.GetExcelSheet<World>()
                    .FirstOrDefault(x => x.RowId == args.ActorWorld);

                if (world == null)
                    return;

                var playerName = $"{args.Text}@{world.Name}";

                this.Plugin.SearchPlayer(playerName);

                //args.Items.Add(new NormalContextMenuItem(this.Plugin.Configuration.ButtonName, Search)); // TODO ContextMenu
            }catch (Exception e)
            {
                PluginLog.Error(e, "Exception hello from open");
                this.Plugin.Pi.Framework.Gui.Chat.PrintChat(new XivChatEntry {
                    MessageBytes = Encoding.UTF8.GetBytes("ContextMenuOpen died, check log."),
                    Type = XivChatType.Urgent,
                });
                throw;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private void Search(ContextMenuItemSelectedArgs args)
        {
            try
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
            catch (Exception e)
            {
                PluginLog.Error(e, "Exception hello from search");
                this.Plugin.Pi.Framework.Gui.Chat.PrintChat(new XivChatEntry {
                    MessageBytes = Encoding.UTF8.GetBytes("ContextMenuSearch died, check log."),
                    Type = XivChatType.Urgent,
                });
                throw;
            }
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
                    return args.Text != null && args.ActorWorld != 0 && args.ActorWorld != 65535;

                default:
                    return false;
            }
        }
    }
}