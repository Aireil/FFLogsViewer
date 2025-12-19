using System;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFLogsViewer.Manager;

public unsafe class OpenWithManager
{
    public bool HasLoadingFailed;
    public bool HasBeenEnabled;

    private DateTime wasOpenedLast = DateTime.Now;
    private short isJoiningPartyFinderOffset;
    private nint charaCardAtkCreationAddress;
    private nint processInspectPacketAddress;
    private nint socialDetailAtkCreationAddress;
    private nint processPartyFinderDetailPacketAddress;
    private nint atkUnitBaseFinalizeAddress;

    private delegate void* CharaCardAtkCreationDelegate(AgentCharaCard* agentCharaCard);
    private Hook<CharaCardAtkCreationDelegate>? charaCardAtkCreationHook;

    private delegate void* ProcessInspectPacketDelegate(Inspect* inspect, void* a2, nint packetData);
    private Hook<ProcessInspectPacketDelegate>? processInspectPacketHook;

    private delegate void* SocialDetailAtkCreationDelegate(void* someAgent, InfoProxyCommonList.CharacterData* data, long a3, void* a4);
    private Hook<SocialDetailAtkCreationDelegate>? socialDetailAtkCreationHook;

    private delegate void* ProcessPartyFinderDetailPacketDelegate(nint someAgent, AgentLookingForGroup.Detailed* data);
    private Hook<ProcessPartyFinderDetailPacketDelegate>? processPartyFinderDetailPacketHook;

    private delegate void AtkUnitBaseFinalizeDelegate(AtkUnitBase* addon);
    private Hook<AtkUnitBaseFinalizeDelegate>? atkUnitBaseFinalizeHook;

    public OpenWithManager()
    {
        this.ScanSigs();
        this.EnableHooks();
    }

    public void ReloadState()
    {
        this.EnableHooks();
    }

    public void Dispose()
    {
        this.charaCardAtkCreationHook?.Dispose();
        this.processInspectPacketHook?.Dispose();
        this.socialDetailAtkCreationHook?.Dispose();
        this.processPartyFinderDetailPacketHook?.Dispose();
        this.atkUnitBaseFinalizeHook?.Dispose();
    }

    private static bool IsEnabled()
    {
        if (Service.Configuration.OpenWith.Key != VirtualKey.NO_KEY)
        {
            if (Service.Configuration.OpenWith.IsDisabledWhenKeyHeld)
            {
                if (Service.KeyState[Service.Configuration.OpenWith.Key])
                {
                    return false;
                }
            }
            else
            {
                if (!Service.KeyState[Service.Configuration.OpenWith.Key])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void Open(string fullName, ushort worldId)
    {
        if (!IsEnabled())
        {
            return;
        }

        var world = Util.GetWorld(worldId);
        if (!Util.IsWorldValid(world))
        {
            return;
        }

        if (fullName == string.Empty)
        {
            return;
        }

        if (Service.Configuration.OpenWith.ShouldIgnoreSelf
            && Service.PlayerState.CharacterName == fullName
            && Service.PlayerState.HomeWorld.RowId == worldId)
        {
            return;
        }

        if (Service.Configuration.OpenWith.ShouldOpenMainWindow)
        {
            this.wasOpenedLast = DateTime.Now;
            Service.MainWindow.Open(false);
            Service.MainWindow.ResetSize();
        }

        if (Service.MainWindow.IsOpen)
        {
            Service.CharDataManager.DisplayedChar.FetchCharacter(fullName, worldId);
        }
    }

    private void ScanSigs()
    {
        try
        {
            this.charaCardAtkCreationAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 74 24 48 48 8B 5C 24 40 48 83 C4 30 5F C3 66 90");
            this.processInspectPacketAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B6 07 84 C0 74 11");
            this.socialDetailAtkCreationAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 8B ?? 0F 00 00 BF 00 00 00 E0");
            this.processPartyFinderDetailPacketAddress = Service.SigScanner.ScanText("E9 ?? ?? ?? ?? CC CC CC CC CC CC 48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 48 8D AC 24");
            this.atkUnitBaseFinalizeAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 45 33 C9 8D 57 01 41 B8");

            try
            {
                this.isJoiningPartyFinderOffset = *(short*)Service.SigScanner.ScanModule("?? ?? ?? ?? ?? ?? 8B D7 48 8D 44 24 ?? 33 C9 89");
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, "isJoiningPartyFinderOffset sig scan failed.");
            }
        }
        catch (Exception ex)
        {
            this.HasLoadingFailed = true;
            Service.PluginLog.Error(ex, "OpenWith sig scan failed.");
        }
    }

    private void EnableHooks()
    {
        if (this.HasLoadingFailed || this.HasBeenEnabled || !Service.Configuration.OpenWith.IsAnyEnabled())
        {
            return;
        }

        try
        {
            this.charaCardAtkCreationHook = Service.GameInteropProvider.HookFromAddress<CharaCardAtkCreationDelegate>(this.charaCardAtkCreationAddress, this.CharaCardAtkCreationDetour);
            this.charaCardAtkCreationHook.Enable();

            this.processInspectPacketHook = Service.GameInteropProvider.HookFromAddress<ProcessInspectPacketDelegate>(this.processInspectPacketAddress, this.ProcessInspectPacketDetour);
            this.processInspectPacketHook.Enable();

            this.socialDetailAtkCreationHook = Service.GameInteropProvider.HookFromAddress<SocialDetailAtkCreationDelegate>(this.socialDetailAtkCreationAddress, this.SocialDetailAtkCreationDetour);
            this.socialDetailAtkCreationHook.Enable();

            this.processPartyFinderDetailPacketHook = Service.GameInteropProvider.HookFromAddress<ProcessPartyFinderDetailPacketDelegate>(this.processPartyFinderDetailPacketAddress, this.ProcessPartyFinderDetailPacketDetour);
            this.processPartyFinderDetailPacketHook.Enable();

            this.atkUnitBaseFinalizeHook = Service.GameInteropProvider.HookFromAddress<AtkUnitBaseFinalizeDelegate>(this.atkUnitBaseFinalizeAddress, this.AktUnitBaseFinalizeDetour);
            this.atkUnitBaseFinalizeHook.Enable();
        }
        catch (Exception ex)
        {
            this.HasLoadingFailed = true;
            Service.PluginLog.Error(ex, "OpenWith hooks enabling failed.");
        }

        this.HasBeenEnabled = true;
    }

    private void* CharaCardAtkCreationDetour(AgentCharaCard* agentCharaCard)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsAdventurerPlateEnabled
                && Service.GameGui.GetAddonByName("BannerEditor") == nint.Zero
                && Service.GameGui.GetAddonByName("CharaCardDesignSetting") == nint.Zero)
            {
                var fullName = agentCharaCard->Data->Name.ToString();
                var worldId = agentCharaCard->Data->WorldId;

                this.Open(fullName, worldId);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in CharaCardAtkCreationDetour.");
        }

        return this.charaCardAtkCreationHook!.Original(agentCharaCard);
    }

    private void* ProcessInspectPacketDetour(Inspect* inspect, void* a2, nint packetData)
    {
        // execute the original function first so the values get assigned in inspect
        var original = this.processInspectPacketHook!.Original(inspect, a2, packetData);

        try
        {
            if (Service.Configuration.OpenWith.IsExamineEnabled)
            {
                var fullName = inspect->NameString;
                var worldId = (ushort)inspect->WorldId;

                this.Open(fullName, worldId);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in ProcessInspectPacketDetour.");
        }

        return original;
    }

    private void* SocialDetailAtkCreationDetour(void* someAgent, InfoProxyCommonList.CharacterData* data, long a3, void* a4)
    {
        try
        {
            // a3 != 0 => editing
            if (Service.Configuration.OpenWith.IsSearchInfoEnabled && a3 == 0)
            {
                var fullName = data->NameString;
                var worldId = data->HomeWorld;

                this.Open(fullName, worldId);
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in SocialDetailAtkCreationDetour.");
        }

        return this.socialDetailAtkCreationHook!.Original(someAgent, data, a3, a4);
    }

    private void* ProcessPartyFinderDetailPacketDetour(nint someAgent, AgentLookingForGroup.Detailed* data)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsPartyFinderEnabled)
            {
                var hasFailed = data->LastPatchHotfixTimestamp == 0; // previously 92/0x5C was checked, but that's not documented in CS yet. this works as a replacement.
                var isPrivate = data->JoinConditionFlags.HasFlag(AgentLookingForGroup.JoinCondition.PrivateParty);
                var isJoining = this.isJoiningPartyFinderOffset != 0 && *(byte*)(someAgent + this.isJoiningPartyFinderOffset) != 0;

                if (!hasFailed && !isPrivate && !isJoining)
                {
                    var fullName = data->LeaderString;
                    var worldId = data->HomeWorld; // is not used in the function, just search it again if it breaks

                    this.Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in ProcessPartyFinderDetailPacketDetour.");
        }

        return this.processPartyFinderDetailPacketHook!.Original(someAgent, data);
    }

    private void AktUnitBaseFinalizeDetour(AtkUnitBase* addon)
    {
        try
        {
            if (IsEnabled() && Service.Configuration.OpenWith.ShouldCloseMainWindow)
            {
                if ((Service.Configuration.OpenWith.IsAdventurerPlateEnabled && addon->NameString == "CharaCard")
                    || (Service.Configuration.OpenWith.IsExamineEnabled && addon->NameString == "CharacterInspect")
                    || (Service.Configuration.OpenWith.IsSearchInfoEnabled && addon->NameString == "SocialDetailB")
                    || (Service.Configuration.OpenWith.IsPartyFinderEnabled && addon->NameString == "LookingForGroupDetail"))
                {
                    // do not close the window if it was just opened, avoid issue of race condition with the addon closing
                    if (DateTime.Now - this.wasOpenedLast > TimeSpan.FromMilliseconds(100))
                    {
                        Service.MainWindow.IsOpen = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Exception in AktUnitBaseFinalizeDetour.");
        }

        this.atkUnitBaseFinalizeHook!.Original(addon);
    }
}
