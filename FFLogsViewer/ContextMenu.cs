using System;
using System.Linq;
using Dalamud.ContextMenu;
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
        Service.ContextMenu.OnOpenGameObjectContextMenu -= OnOpenContextMenu;
        Service.ContextMenu.OnOpenGameObjectContextMenu += OnOpenContextMenu;
    }

    public static void Disable()
    {
        Service.ContextMenu.OnOpenGameObjectContextMenu -= OnOpenContextMenu;
    }

    public void Dispose()
    {
        Disable();
        GC.SuppressFinalize(this);
    }

    private static bool IsMenuValid(BaseContextMenuArgs args)
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
            case "BeginnerChatList":
                return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;

            default:
                return false;
        }
    }

    private static void SearchPlayerFromMenu(BaseContextMenuArgs args)
    {
        var world = Service.DataManager.GetExcelSheet<World>()
                           ?.FirstOrDefault(x => x.RowId == args.ObjectWorld);

        if (world == null)
            return;

        var playerName = $"{args.Text}@{world.Name}";

        if (Service.Configuration.OpenInBrowser && !Service.Configuration.ContextMenuStreamer)
        {
            CharDataManager.OpenCharInBrowser(playerName);
        }
        else
        {
            if (!Service.MainWindow.IsOpen)
            {
                Service.MainWindow.ResetTemporarySettings();
            }

            Service.CharDataManager.DisplayedChar.FetchTextCharacter(playerName);
            Service.MainWindow.IsOpen = true;
        }
    }

    private static void OnOpenContextMenu(GameObjectContextMenuOpenArgs args)
    {
        if (!Service.Interface.UiBuilder.ShouldModifyUi || !IsMenuValid(args))
            return;

        if (Service.Configuration.ContextMenuStreamer)
        {
            if (!Service.MainWindow.IsOpen)
                return;

            SearchPlayerFromMenu(args);
        }
        else
        {
            args.AddCustomItem(new GameObjectContextMenuItem(Service.Configuration.ContextMenuButtonName ?? "Search FF Logs", Search));
        }
    }

    private static void Search(GameObjectContextMenuItemSelectedArgs args)
    {
        if (!IsMenuValid(args))
                return;

        SearchPlayerFromMenu(args);
    }
}
