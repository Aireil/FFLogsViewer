﻿using System;
using System.Linq;
using Dalamud.Game.Gui.ContextMenus;
using FFLogsViewer.Manager;
using Lumina.Excel.GeneratedSheets;

namespace FFLogsViewer;

public class ContextMenu : IDisposable
{
        public ContextMenu()
        {
            if (Service.Configuration.ContextMenu)
            {
                Enable();
            }
        }

        public static void Enable()
        {
            Service.ContextMenu.ContextMenuOpened -= OnOpenContextMenu;
            Service.ContextMenu.ContextMenuOpened += OnOpenContextMenu;
        }

        public static void Disable()
        {
            Service.ContextMenu.ContextMenuOpened -= OnOpenContextMenu;
        }

        public void Dispose()
        {
            Disable();
            GC.SuppressFinalize(this);
        }

        private static void OnOpenContextMenu(ContextMenuOpenedArgs args)
        {
            if (!IsMenuValid(args))
            {
                return;
            }

            if (Service.Configuration.ContextMenuStreamer)
            {
                if (!Service.MainWindow.IsOpen)
                {
                    return;
                }

                SearchPlayerFromMenu(args);
            }
            else
            {
                args.AddCustomItem(Service.Configuration.ContextMenuButtonName, Search);
            }
        }

        private static bool IsMenuValid(ContextMenuOpenedArgs args)
        {
            switch (args.ParentAddonName)
            {
                case null: // Nameplate/Model menu
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "FreeCompany":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                case "_PartyList":
                case "LinkShell":
                case "CrossWorldLinkshell":
                case "ContentMemberList": // Eureka/Bozja/...
                    return args.GameObjectContext?.Name != null && args.GameObjectContext?.WorldId != 0 && args.GameObjectContext?.WorldId != 65535;

                default:
                    return false;
            }
        }

        private static void SearchPlayerFromMenu(ContextMenuOpenedArgs args)
        {
            var world = Service.DataManager.GetExcelSheet<World>()
                               ?.FirstOrDefault(x => x.RowId == args.GameObjectContext?.WorldId);

            if (world == null)
                return;

            var playerName = $"{args.GameObjectContext?.Name}@{world.Name}";

            if (Service.Configuration.OpenInBrowser && !Service.Configuration.ContextMenuStreamer)
            {
                CharDataManager.OpenCharInBrowser(playerName);
            }
            else
            {
                Service.CharDataManager.DisplayedChar.FetchTextCharacter(playerName);
                Service.MainWindow.IsOpen = true;
            }
        }

        private static void Search(CustomContextMenuItemSelectedArgs args)
        {
            if (!IsMenuValid(args.ContextMenuOpenedArgs))
            {
                return;
            }

            SearchPlayerFromMenu(args.ContextMenuOpenedArgs);
        }
}
