using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Memory;
using FFLogsViewer.Manager;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace FFLogsViewer;

public class ContextMenu
{
    public static void Enable()
    {
        Service.ContextMenu.OnMenuOpened += OnOpenContextMenu;
    }

    public static void Disable()
    {
        Service.ContextMenu.OnMenuOpened -= OnOpenContextMenu;
    }

    private static bool IsMenuValid(IMenuArgs menuOpenedArgs)
    {
        if (menuOpenedArgs.Target is not MenuTargetDefault menuTargetDefault)
        {
            return false;
        }

        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (menuOpenedArgs.AddonName)
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
                return menuTargetDefault.TargetName != string.Empty && Util.IsWorldValid(menuTargetDefault.TargetHomeWorld.RowId);

            case "BlackList":
            case "MuteList":
                return menuTargetDefault.TargetName != string.Empty;
        }

        return false;
    }

    private static void SearchPlayerFromMenu(IMenuArgs menuArgs)
    {
        if (menuArgs.Target is not MenuTargetDefault menuTargetDefault)
        {
            return;
        }

        string playerName;
        if (menuArgs.AddonName == "BlackList")
        {
            playerName = GetBlacklistSelectFullName();
        }
        else if (menuArgs.AddonName == "MuteList")
        {
            playerName = GetMuteListSelectFullName();
        }
        else
        {
            var world = Util.GetWorld(menuTargetDefault.TargetHomeWorld.RowId);
            if (!Util.IsWorldValid(world))
            {
                return;
            }

            playerName = $"{menuTargetDefault.TargetName}@{world.Name}";
        }

        if (Service.Configuration is { OpenInBrowser: true, ContextMenuStreamer: false })
        {
            CharDataManager.OpenCharInBrowser(playerName);
        }
        else
        {
            Service.MainWindow.Open();
            if (Service.Configuration.ContextMenuAlwaysPartyView
                || (Service.Configuration.ContextMenuPartyView && IsPartyAddon(menuArgs.AddonName)))
            {
                Service.MainWindow.IsPartyView = true;
                Service.CharDataManager.UpdatePartyMembers();
                Service.CharDataManager.DisplayedChar.ParseTextForChar(playerName);
            }
            else
            {
                Service.CharDataManager.DisplayedChar.FetchCharacter(playerName);
            }
        }
    }

    private static void OnOpenContextMenu(IMenuOpenedArgs menuOpenedArgs)
    {
        if (!Service.Interface.UiBuilder.ShouldModifyUi || !IsMenuValid(menuOpenedArgs))
        {
            return;
        }

        if (Service.Configuration.ContextMenuStreamer)
        {
            if (!Service.MainWindow.IsOpen)
            {
                return;
            }

            SearchPlayerFromMenu(menuOpenedArgs);
        }
        else
        {
            menuOpenedArgs.AddMenuItem(new MenuItem
            {
                PrefixChar = 'F',
                Name = Service.Configuration.ContextMenuButtonName,
                OnClicked = Search,
            });
        }
    }

    private static void Search(IMenuItemClickedArgs menuItemClickedArgs)
    {
        if (!IsMenuValid(menuItemClickedArgs))
        {
            return;
        }

        SearchPlayerFromMenu(menuItemClickedArgs);
    }

    private static unsafe string GetBlacklistSelectFullName()
    {
        var agentBlackList = AgentBlacklist.Instance();
        if (agentBlackList != null)
        {
            return MemoryHelper.ReadSeString(&agentBlackList->SelectedPlayerFullName).TextValue;
        }

        return string.Empty;
    }

    private static unsafe string GetMuteListSelectFullName()
    {
        var agentMuteList = AgentMutelist.Instance();
        if (agentMuteList != null)
        {
            return MemoryHelper.ReadSeString(&agentMuteList->SelectedPlayerFullName).TextValue;
        }

        return string.Empty;
    }

    private static bool IsPartyAddon(string? menuArgsAddonName)
    {
        return menuArgsAddonName switch
        {
            "PartyMemberList" => true,
            "_PartyList" => true,
            _ => false,
        };
    }
}
