using System;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFLogsViewer.Manager;

public unsafe class OpenWithManager
{
    public bool HasLoadingFailed;
    public bool HasBeenEnabled;

    private DateTime wasOpenedLast = DateTime.Now;
    private IntPtr charaCardAtkCreationAddress;
    private IntPtr processInspectPacketAddress;
    private IntPtr socialDetailAtkCreationAddress;
    private IntPtr processPartyFinderDetailPacketAddress;
    private IntPtr atkUnitBaseFinalizeAddress;

    private delegate void* CharaCardAtkCreationDelegate(IntPtr agentCharaCard);
    private Hook<CharaCardAtkCreationDelegate>? charaCardAtkCreationHook;

    private delegate void* ProcessInspectPacketDelegate(void* someAgent, void* a2, IntPtr packetData);
    private Hook<ProcessInspectPacketDelegate>? processInspectPacketHook;

    private delegate void* SocialDetailAtkCreationDelegate(void* someAgent, IntPtr data, long a3, void* a4);
    private Hook<SocialDetailAtkCreationDelegate>? socialDetailAtkCreationHook;

    private delegate void* ProcessPartyFinderDetailPacketDelegate(IntPtr something, IntPtr packetData);
    private Hook<ProcessPartyFinderDetailPacketDelegate>? processPartyFinderDetailPacketHook;

    private delegate void* AtkUnitBaseFinalizeDelegate(AtkUnitBase* addon);
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

    private void Open(SeString fullName, ushort worldId)
    {
        if (!IsEnabled())
        {
            return;
        }

        if (Service.Configuration.OpenWith.ShouldIgnoreSelf
            && Service.ClientState.LocalPlayer?.Name.TextValue == fullName.TextValue
            && Service.ClientState.LocalPlayer?.HomeWorld.Id == worldId)
        {
            return;
        }

        if (Service.Configuration.OpenWith.ShouldOpenMainWindow)
        {
            this.wasOpenedLast = DateTime.Now;
            Service.MainWindow.Open();
            Service.MainWindow.ResetSize();
        }

        if (Service.MainWindow.IsOpen)
        {
            Service.CharDataManager.DisplayedChar.FetchCharacter(fullName.TextValue, worldId);
        }
    }

    private void ScanSigs()
    {
        try
        {
            // AgentCharaCard_Update => state == 3 => last function => last function
            this.charaCardAtkCreationAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 4C 8B 74 24 ?? 48 8B 74 24 ?? 48 8B 5C 24 ?? 48 83 C4 30");

            // Get opcode from examining someone and look in the big switch
            this.processInspectPacketAddress = Service.SigScanner.ScanText("48 89 5C 24 ?? 56 41 56 41 57 48 83 EC 20 8B DA");

            // Look what accesses social detail agent when opening search info
            this.socialDetailAtkCreationAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? BE");

            // Client::UI::UIModule_vf109 in 6.28, look what accesses a4 when opening a PF
            this.processPartyFinderDetailPacketAddress = Service.SigScanner.ScanText("E9 ?? ?? ?? ?? CC CC CC CC CC CC 48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 48 8D AC 24");

            this.atkUnitBaseFinalizeAddress = Service.SigScanner.ScanText("E8 ?? ?? ?? ?? 45 33 C9 8D 57 01 41 B8");
        }
        catch (Exception ex)
        {
            this.HasLoadingFailed = true;
            PluginLog.Error(ex, "OpenWith sig scan failed.");
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
            this.charaCardAtkCreationHook = Hook<CharaCardAtkCreationDelegate>.FromAddress(this.charaCardAtkCreationAddress, this.CharaCardAtkCreationDetour);
            this.charaCardAtkCreationHook.Enable();

            this.processInspectPacketHook = Hook<ProcessInspectPacketDelegate>.FromAddress(this.processInspectPacketAddress, this.ProcessInspectPacketDetour);
            this.processInspectPacketHook.Enable();

            this.socialDetailAtkCreationHook = Hook<SocialDetailAtkCreationDelegate>.FromAddress(this.socialDetailAtkCreationAddress, this.SocialDetailAtkCreationDetour);
            this.socialDetailAtkCreationHook.Enable();

            this.processPartyFinderDetailPacketHook = Hook<ProcessPartyFinderDetailPacketDelegate>.FromAddress(this.processPartyFinderDetailPacketAddress, this.ProcessPartyFinderDetailPacketDetour);
            this.processPartyFinderDetailPacketHook.Enable();

            this.atkUnitBaseFinalizeHook = Hook<AtkUnitBaseFinalizeDelegate>.FromAddress(this.atkUnitBaseFinalizeAddress, this.AktUnitBaseFinalizeDetour);
            this.atkUnitBaseFinalizeHook.Enable();
        }
        catch (Exception ex)
        {
            this.HasLoadingFailed = true;
            PluginLog.Error(ex, "OpenWith hooks enabling failed.");
        }

        this.HasBeenEnabled = true;
    }

    private void* CharaCardAtkCreationDetour(IntPtr agentCharaCard)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsAdventurerPlateEnabled
                && Service.GameGui.GetAddonByName("BannerEditor", 1) == IntPtr.Zero
                && Service.GameGui.GetAddonByName("CharaCardDesignSetting", 1) == IntPtr.Zero)
            {
                // To get offsets: 6.21 process chara card network packet 40 55 53 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 79 ?? ?? 48 8B DA
                var worldId = *(ushort*)(*(IntPtr*)(agentCharaCard + 40) + 192);
                if (worldId != 0 && worldId != 65535)
                {
                    var fullName = MemoryHelper.ReadSeStringNullTerminated(*(IntPtr*)(*(IntPtr*)(agentCharaCard + 40) + 88));

                    this.Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception in CharaCardAtkCreationDetour.");
        }

        return this.charaCardAtkCreationHook!.Original(agentCharaCard);
    }

    private void* ProcessInspectPacketDetour(void* someAgent, void* a2, IntPtr packetData)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsExamineEnabled)
            {
                // To get offsets: 6.21 process inspect network packet 48 89 5C 24 ?? 56 41 56 41 57 48 83 EC 20 8B DA
                var worldId = *(ushort*)(packetData + 50);
                if (worldId != 0 && worldId != 65535)
                {
                    var fullName = MemoryHelper.ReadSeStringNullTerminated(packetData + 624);

                    this.Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception in ProcessInspectPacketDetour.");
        }

        return this.processInspectPacketHook!.Original(someAgent, a2, packetData);
    }

    private void* SocialDetailAtkCreationDetour(void* someAgent, IntPtr data, long a3, void* a4)
    {
        try
        {
            // a3 != 0 => editing
            if (Service.Configuration.OpenWith.IsSearchInfoEnabled && a3 == 0)
            {
                // To get offsets: look pointed memory by a2 in CE
                var worldId = *(ushort*)(data + 24);
                if (worldId != 0 && worldId != 65535)
                {
                    var fullName = MemoryHelper.ReadSeStringNullTerminated(data + 34);

                    this.Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception in SocialDetailAtkCreationDetour.");
        }

        return this.socialDetailAtkCreationHook!.Original(someAgent, data, a3, a4);
    }

    private void* ProcessPartyFinderDetailPacketDetour(IntPtr something, IntPtr packetData)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsPartyFinderEnabled)
            {
                // To get offsets: 6.28, look in this function
                var hasFailed = *(byte*)(packetData + 84) == 0; // (*(byte*)(packetData + 83) & 1) == 0 for World parties (?)
                var isJoining = *(byte*)(something + 11169) != 0;

                if (!hasFailed && !isJoining)
                {
                    var worldId = *(ushort*)(packetData + 74); // is not used in the function, just search it again if it breaks
                    if (worldId != 0 && worldId != 65535)
                    {
                        var fullName = MemoryHelper.ReadSeStringNullTerminated(packetData + 712);

                        this.Open(fullName, worldId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception in ProcessPartyFinderDetailPacketDetour.");
        }

        return this.processPartyFinderDetailPacketHook!.Original(something, packetData);
    }

    private void* AktUnitBaseFinalizeDetour(AtkUnitBase* addon)
    {
        try
        {
            if (IsEnabled() && Service.Configuration.OpenWith.ShouldCloseMainWindow)
            {
                if ((Service.Configuration.OpenWith.IsAdventurerPlateEnabled && MemoryHelper.ReadSeStringNullTerminated((IntPtr)addon->Name).TextValue == "CharaCard")
                    || (Service.Configuration.OpenWith.IsExamineEnabled && MemoryHelper.ReadSeStringNullTerminated((IntPtr)addon->Name).TextValue == "CharacterInspect")
                    || (Service.Configuration.OpenWith.IsSearchInfoEnabled && MemoryHelper.ReadSeStringNullTerminated((IntPtr)addon->Name).TextValue == "SocialDetailB")
                    || (Service.Configuration.OpenWith.IsPartyFinderEnabled && MemoryHelper.ReadSeStringNullTerminated((IntPtr)addon->Name).TextValue == "LookingForGroupDetail"))
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
            PluginLog.Error(ex, "Exception in AktUnitBaseFinalizeDetour.");
        }

        return this.atkUnitBaseFinalizeHook!.Original(addon);
    }
}
