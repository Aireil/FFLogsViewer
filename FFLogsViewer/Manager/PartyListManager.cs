using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Excel.GeneratedSheets;
using PartyMember = FFLogsViewer.Model.PartyMember;

namespace FFLogsViewer.Manager;

public class PartyListManager : IDisposable
{
    public List<PartyMember> PartyList = new();
    private List<TextureWrap>? jobIcons;
    private int iconLoadAttemptsLeft = 4;

    public void UpdatePartyList()
    {
        this.PartyList = GetPartyMembers();
    }

    public TextureWrap? GetJobIcon(uint jobId)
    {
        if (this.jobIcons == null)
        {
            this.LoadJobIcons();
        }

        if (this.jobIcons is { Count: 41 } && jobId is >= 0 and <= 40)
        {
            return this.jobIcons[(int)jobId];
        }

        return null;
    }

    public void Dispose()
    {
        if (this.jobIcons != null)
        {
            foreach (var icon in this.jobIcons)
            {
                icon.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }

    private static unsafe List<PartyMember> GetPartyMembers()
    {
        var partyMembers = new List<PartyMember>();

        var groupManager = GroupManager.Instance();
        if (groupManager->MemberCount > 0)
        {
            AddMembersFromGroupManager(partyMembers, groupManager);
        }
        else
        {
            var cwProxy = InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0)
            {
                var localIndex = cwProxy->LocalPlayerGroupIndex;
                AddMembersFromCRGroup(partyMembers, cwProxy->CrossRealmGroupSpan[localIndex]);

                for (var i = 0; i < cwProxy->CrossRealmGroupSpan.Length; i++)
                {
                    if (i == localIndex)
                    {
                        continue;
                    }

                    AddMembersFromCRGroup(partyMembers, cwProxy->CrossRealmGroupSpan[i]);
                }
            }
        }

        if (partyMembers.Count == 0)
        {
            var selfName = Service.ClientState.LocalPlayer?.Name;
            var selfWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id;
            var selfWorld = Service.DataManager.GetExcelSheet<World>()
                                   ?.FirstOrDefault(x => x.RowId == selfWorldId);
            var selfJobId = Service.ClientState.LocalPlayer?.ClassJob.Id;
            if (selfName != null && selfWorld != null && selfJobId != null)
            {
                partyMembers.Add(new PartyMember { Name = selfName.ToString(), World = selfWorld.Name, JobId = selfJobId.Value });
            }
        }

        return partyMembers;
    }

    private static unsafe void AddMembersFromCRGroup(ICollection<PartyMember> partyMembers, CrossRealmGroup crossRealmGroup)
    {
        foreach (var groupMember in crossRealmGroup.GroupMemberSpan)
        {
            var worldId = groupMember.HomeWorld;
            var world = Service.DataManager.GetExcelSheet<World>()
                               ?.FirstOrDefault(x => x.RowId == worldId);
            if (world == null)
            {
                continue;
            }

            var name = Util.ReadSeString(groupMember.Name);
            partyMembers.Add(new PartyMember { Name = name.ToString(), World = world.Name, JobId = groupMember.ClassJobId });
        }
    }

    private static unsafe void AddMembersFromGroupManager(ICollection<PartyMember> partyMembers, GroupManager* groupManager)
    {
        for (var i = 0; i < groupManager->MemberCount; i++)
        {
            var groupMember = groupManager->GetPartyMemberByIndex(i);

            var partyMember = GetPartyMember(groupMember);
            if (partyMember != null && partyMember.Name != string.Empty && partyMember.World != string.Empty)
            {
                partyMembers.Add(partyMember);
            }
        }

        for (var i = 0; i < 20; i++)
        {
            var allianceMember = groupManager->GetAllianceMemberByIndex(i);

            var partyMember = GetPartyMember(allianceMember);
            if (partyMember != null && partyMember.Name != string.Empty && partyMember.World != string.Empty)
            {
                partyMembers.Add(partyMember);
            }
        }
    }

    private static unsafe PartyMember? GetPartyMember(
        FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember* partyMember)
    {
        if (partyMember == null)
        {
            return null;
        }

        var worldId = partyMember->HomeWorld;
        var world = Service.DataManager.GetExcelSheet<World>()
                           ?.FirstOrDefault(x => x.RowId == worldId);
        if (world == null)
        {
            return null;
        }

        var name = Util.ReadSeString(partyMember->Name);
        return new PartyMember { Name = name.ToString(), World = world.Name, JobId = partyMember->ClassJob };
    }

    private static TextureWrap? GetIconTextureWrap(int id)
    {
        try
        {
            TexFile? iconTex = null;
            var iconPath = $"ui/icon/062000/0{id}_hr1.tex";
            if (IPC.PenumbraEnabled)
            {
                try
                {
                    iconTex = Service.DataManager.GameData.GetFileFromDisk<TexFile>(IPC.ResolvePenumbraPath(iconPath));
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            iconTex ??= Service.DataManager.GetFile<TexFile>(iconPath);

            if (iconTex != null)
            {
                var tex = Service.Interface.UiBuilder.LoadImageRaw(iconTex.GetRgbaImageData(), iconTex.Header.Width, iconTex.Header.Height, 4);
                if (tex.ImGuiHandle != IntPtr.Zero)
                {
                    return tex;
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Icon loading failed.");
        }

        return null;
    }

    private void LoadJobIcons()
    {
        if (this.iconLoadAttemptsLeft <= 0)
        {
            return;
        }

        this.jobIcons = new List<TextureWrap>();
        var hasFailed = false;

        var defaultIcon = GetIconTextureWrap(62143);
        if (defaultIcon != null)
        {
            this.jobIcons.Add(defaultIcon);
        }
        else
        {
            hasFailed = true;
        }

        for (var i = 62101; i <= 62140 && !hasFailed; i++)
        {
            var icon = GetIconTextureWrap(i);
            if (icon != null)
            {
                this.jobIcons.Add(icon);
            }
            else
            {
                hasFailed = true;
            }
        }

        if (hasFailed)
        {
            if (this.jobIcons != null)
            {
                foreach (var icon in this.jobIcons)
                {
                    icon.Dispose();
                }
            }

            this.jobIcons = null;

            PluginLog.Error($"Job icons loading failed, {--this.iconLoadAttemptsLeft} attempt(s) left.");
        }
    }
}
