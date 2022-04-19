using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using FFLogsViewer.Manager;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class HeaderBar
{
    public string ErrorMessage = string.Empty;
    public uint ResetSizeCount;

    private bool isProfileLinkClicked;
    private bool isConfigClicked;
    private List<string> partyList = new();

    public void Draw()
    {
        if (this.isConfigClicked)
        {
            Service.ConfigWindow.IsOpen = true;
            this.isConfigClicked = false;
        }

        if (this.isProfileLinkClicked)
        {
            Util.OpenLink(Service.CharDataManager.DisplayedChar);
            this.isProfileLinkClicked = false;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));

        var buttonsWidth = GetButtonsWidth();
        var minWindowSize = GetMinWindowSize();

        if (ImGui.GetWindowSize().X < minWindowSize || this.ResetSizeCount != 0)
        {
            ImGui.SetWindowSize(new Vector2(minWindowSize, -1));
        }

        if (!Service.Configuration.Style.IsSizeFixed
            && (Service.Configuration.Style.MainWindowFlags & ImGuiWindowFlags.AlwaysAutoResize) == 0)
        {
            ImGui.SetWindowSize(new Vector2(Service.Configuration.Style.MinMainWindowWidth > minWindowSize ? Service.Configuration.Style.MinMainWindowWidth : minWindowSize, -1));
        }

        // I hate ImGui
        var contentRegionAvailWidth = ImGui.GetContentRegionAvail().X;
        if (ImGui.GetWindowSize().X < minWindowSize || this.ResetSizeCount != 0)
        {
            contentRegionAvailWidth = minWindowSize - (ImGui.GetStyle().WindowPadding.X * 2);
            this.ResetSizeCount--;
        }

        var calcInputSize = (contentRegionAvailWidth - (ImGui.GetStyle().ItemSpacing.X * 2) - buttonsWidth) / 3;

        ImGui.SetNextItemWidth(calcInputSize);
        ImGui.InputTextWithHint("##FirstName", "First Name", ref Service.CharDataManager.DisplayedChar.FirstName, 15, ImGuiInputTextFlags.CharsNoBlank);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(calcInputSize);
        ImGui.InputTextWithHint("##LastName", "Last Name", ref Service.CharDataManager.DisplayedChar.LastName, 15, ImGuiInputTextFlags.CharsNoBlank);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(calcInputSize);
        ImGui.InputTextWithHint("##WorldName", "World", ref Service.CharDataManager.DisplayedChar.WorldName, 15, ImGuiInputTextFlags.CharsNoBlank);

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Search))
        {
            Service.CharDataManager.DisplayedChar.FetchData();
        }

        Util.SetHoverTooltip("Search");

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Crosshairs))
        {
            Service.CharDataManager.DisplayedChar.FetchTargetChar();
        }

        Util.SetHoverTooltip("Target");

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Clipboard))
        {
            Service.CharDataManager.DisplayedChar.FetchClipboardCharacter();
        }

        Util.SetHoverTooltip("Search clipboard");

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.UsersCog))
        {
            this.partyList = CharDataManager.GetPartyMembers();
            ImGui.OpenPopup("##PartyList");
        }

        Util.SetHoverTooltip("Party members");

        if (ImGui.BeginPopup("##PartyList", ImGuiWindowFlags.NoMove))
        {
            if (this.partyList.Count != 0)
            {
                for (var i = 0; i < this.partyList.Count; i++)
                {
                    var partyMember = this.partyList[i];
                    if (ImGui.Selectable($"{partyMember}##{i}"))
                    {
                        Service.CharDataManager.DisplayedChar.FetchTextCharacter(partyMember);
                    }
                }
            }
            else
            {
                ImGui.Text("Not in a party");
            }

            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2);

        if (!Service.FfLogsClient.IsTokenValid)
        {
            var message = FFLogsClient.IsConfigSet()
                              ? "API client not valid, click to open settings."
                              : "API client not setup, click to open settings.";
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            Util.CenterSelectable(message, ref this.isConfigClicked);
            ImGui.PopStyleColor();

            return;
        }

        if (this.ErrorMessage == string.Empty)
        {
            if (Service.CharDataManager.DisplayedChar.IsDataLoading)
            {
                Util.CenterText("Loading...");
            }
            else
            {
                if (Service.CharDataManager.DisplayedChar.IsDataReady)
                {
                    Util.CenterSelectable(
                        $"Viewing {Service.CharDataManager.DisplayedChar.LoadedFirstName} {Service.CharDataManager.DisplayedChar.LoadedLastName}@{Service.CharDataManager.DisplayedChar.LoadedWorldName}'s logs",
                        ref this.isProfileLinkClicked);

                    Util.SetHoverTooltip("Click to open on FF Logs");
                }
                else
                {
                    Util.CenterText("Waiting...");
                }
            }
        }
        else
        {
            Util.CenterTextColored(ImGuiColors.DalamudRed, this.ErrorMessage);
        }

        if (Service.Configuration.Layout.Count == 0)
        {
            Util.CenterSelectable("You have no layout set up. Click to open settings.", ref this.isConfigClicked);
        }
    }

    private static float GetButtonsWidth()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        var buttonsWidth =
            ImGui.CalcTextSize(FontAwesomeIcon.Search.ToIconString()).X +
            ImGui.CalcTextSize(FontAwesomeIcon.Crosshairs.ToIconString()).X +
            ImGui.CalcTextSize(FontAwesomeIcon.Clipboard.ToIconString()).X +
            ImGui.CalcTextSize(FontAwesomeIcon.UsersCog.ToIconString()).X +
            (ImGui.GetStyle().ItemSpacing.X * 4) + // between items
            (ImGui.GetStyle().FramePadding.X * 8); // around buttons, 2 per
        ImGui.PopFont();
        return buttonsWidth;
    }

    private static float GetMinInputWidth()
    {
        return new[]
        {
            ImGui.CalcTextSize("First Name").X,
            ImGui.CalcTextSize("Last Name").X,
            ImGui.CalcTextSize("World").X,
            ImGui.CalcTextSize(Service.CharDataManager.DisplayedChar.FirstName).X,
            ImGui.CalcTextSize(Service.CharDataManager.DisplayedChar.LastName).X,
            ImGui.CalcTextSize(Service.CharDataManager.DisplayedChar.WorldName).X,
        }.Max() + (ImGui.GetStyle().FramePadding.X * 2);
    }

    private static float GetMinWindowSize()
    {
        return ((GetMinInputWidth() + (ImGui.GetStyle().ItemSpacing.X * 2)) * 3) + GetButtonsWidth();
    }
}
