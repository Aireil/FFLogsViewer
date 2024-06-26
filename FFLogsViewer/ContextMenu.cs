﻿using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Memory;
using FFLogsViewer.Manager;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
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

    private static bool IsMenuValid(MenuArgs menuOpenedArgs)
    {
        if (menuOpenedArgs.Target is not MenuTargetDefault menuTargetDefault)
        {
            return false;
        }

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
                return menuTargetDefault.TargetName != string.Empty && Util.IsWorldValid(menuTargetDefault.TargetHomeWorld.Id);

            case "BlackList":
                return menuTargetDefault.TargetName != string.Empty;
        }

        return false;
    }

    private static void SearchPlayerFromMenu(MenuArgs menuArgs)
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
        else
        {
            var world = Util.GetWorld(menuTargetDefault.TargetHomeWorld.Id);
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

    private static void OnOpenContextMenu(MenuOpenedArgs menuOpenedArgs)
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

    private static void Search(MenuItemClickedArgs menuItemClickedArgs)
    {
        if (!IsMenuValid(menuItemClickedArgs))
        {
            return;
        }

        SearchPlayerFromMenu(menuItemClickedArgs);
    }

    private static unsafe string GetBlacklistSelectFullName()
    {
        var agentBlackList = (AgentBlacklist*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.SocialBlacklist);
        if (agentBlackList != null)
        {
            return MemoryHelper.ReadSeString(&agentBlackList->SelectedPlayerFullName).TextValue;
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
