using System;
using System.Linq;
using FFLogsViewer.Manager;
using Lumina.Excel.GeneratedSheets;
using XivCommon.Functions.ContextMenu;

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
        Service.Common.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
        Service.Common.Functions.ContextMenu.OpenContextMenu += OnOpenContextMenu;
    }

    public static void Disable()
    {
        Service.Common.Functions.ContextMenu.OpenContextMenu -= OnOpenContextMenu;
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
            Service.CharDataManager.DisplayedChar.FetchTextCharacter(playerName);
            Service.MainWindow.IsOpen = true;
        }
    }

    private static void OnOpenContextMenu(ContextMenuOpenArgs args)
    {
        if (!IsMenuValid(args))
            return;

        if (Service.Configuration.ContextMenuStreamer)
        {
            if (!Service.MainWindow.IsOpen)
                return;

            SearchPlayerFromMenu(args);
        }
        else
        {
            args.Items.Add(new NormalContextMenuItem(Service.Configuration.ContextMenuButtonName ?? "Search FF Logs", Search));
        }
    }

    private static void Search(ContextMenuItemSelectedArgs args)
    {
        if (!IsMenuValid(args))
                return;

        SearchPlayerFromMenu(args);
    }
}
