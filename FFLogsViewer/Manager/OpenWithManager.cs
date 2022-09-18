using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;

namespace FFLogsViewer.Manager;

public unsafe class OpenWithManager
{
    public bool HasLoadingFailed;
    public bool HasBeenEnabled;

    private IntPtr charaCardAtkCreationAddress;
    private IntPtr processInspectPacketAddress;
    private IntPtr socialDetailAtkCreationAddress;

    private delegate void* CharaCardAtkCreationDelegate(IntPtr agentCharaCard);
    private Hook<CharaCardAtkCreationDelegate>? charaCardAtkCreationHook;

    private delegate void* ProcessInspectPacketDelegate(void* someAgent, void* a2, IntPtr packetData);
    private Hook<ProcessInspectPacketDelegate>? processInspectPacketHook;

    private delegate void* SocialDetailAtkCreationDelegate(void* someAgent, IntPtr data, void* a3, void* a4);
    private Hook<SocialDetailAtkCreationDelegate>? socialDetailAtkCreationHook;

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
    }

    private static void Open(SeString fullName, ushort worldId)
    {
        if (Service.Configuration.OpenWith.ShouldOpenMainWindow && !Service.MainWindow.IsOpen)
        {
            Service.MainWindow.Open();
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
            if (Service.Configuration.OpenWith.IsAdventurerPlateEnabled)
            {
                // To get offsets: 6.21 process chara card network packet 40 55 53 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 48 83 79 ?? ?? 48 8B DA
                var worldId = *(ushort*)(*(IntPtr*)(agentCharaCard + 40) + 192);
                if (worldId != 0 && worldId != 65535)
                {
                    var fullName = MemoryHelper.ReadSeStringNullTerminated(*(IntPtr*)(*(IntPtr*)(agentCharaCard + 40) + 88));

                    Open(fullName, worldId);
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

                    Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception in ProcessInspectPacketDetour.");
        }

        return this.processInspectPacketHook!.Original(someAgent, a2, packetData);
    }

    private void* SocialDetailAtkCreationDetour(void* someAgent, IntPtr data, void* a3, void* a4)
    {
        try
        {
            if (Service.Configuration.OpenWith.IsSearchInfoEnabled)
            {
                // To get offsets: look pointed memory by a2 in CE
                var worldId = *(ushort*)(data + 24);
                if (worldId != 0 && worldId != 65535)
                {
                    var fullName = MemoryHelper.ReadSeStringNullTerminated(data + 34);

                    Open(fullName, worldId);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception in SocialDetailAtkCreationDetour.");
        }

        return this.socialDetailAtkCreationHook!.Original(someAgent, data, a3, a4);
    }
}
