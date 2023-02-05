using System.Numerics;
using Dalamud.Interface;
using FFLogsViewer.Manager;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class MenuBar
{
    public static void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));

        if (ImGui.BeginMenuBar())
        {
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.MenuItem(FontAwesomeIcon.Eraser.ToIconString()))
            {
                Service.CharDataManager.ResetDisplayedChar();
                MainWindow.ResetError();
                Service.MainWindow.ResetSize();
            }

            ImGui.PopFont();
            Util.SetHoverTooltip("Clear");

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.MenuItem(FontAwesomeIcon.Cog.ToIconString()))
            {
                Service.ConfigWindow.Toggle();
            }

            ImGui.PopFont();
            Util.SetHoverTooltip("Configuration");

            var swapViewIcon = Service.MainWindow.IsPartyView ? FontAwesomeIcon.User : FontAwesomeIcon.UsersCog;
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.MenuItem(swapViewIcon.ToIconString()))
            {
                Service.MainWindow.IsPartyView = !Service.MainWindow.IsPartyView;
            }

            ImGui.PopFont();
            Util.SetHoverTooltip(Service.MainWindow.IsPartyView ? "Swap to Single View" : "Swap to Party View");

            var hasTmpSettingChanged = false;

            ImGui.PushStyleColor(ImGuiCol.Text, Service.MainWindow.Job.Color);
            if (ImGui.BeginMenu(Service.MainWindow.Job.Abbreviation))
            {
                foreach (var job in Service.GameDataManager.Jobs)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, job.Color);
                    if (ImGui.MenuItem(job.Name))
                    {
                        Service.MainWindow.Job = job;
                        hasTmpSettingChanged = true;
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.EndMenu();
            }

            ImGui.PopStyleColor();

            if (ImGui.BeginMenu(Service.MainWindow.GetCurrentMetric().Abbreviation))
            {
                foreach (var metric in GameDataManager.AvailableMetrics)
                {
                    if (ImGui.MenuItem(metric.Name))
                    {
                        Service.MainWindow.OverriddenMetric = metric;
                        hasTmpSettingChanged = true;
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Service.MainWindow.Partition.Abbreviation))
            {
                foreach (var partition in GameDataManager.AvailablePartitions)
                {
                    if (ImGui.MenuItem(partition.Name))
                    {
                        Service.MainWindow.Partition = partition;
                        hasTmpSettingChanged = true;
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Service.MainWindow.IsTimeframeHistorical() ? "H%" : "T%"))
            {
                if (ImGui.MenuItem("Historical %"))
                {
                    Service.MainWindow.IsOverridingTimeframe = !Service.Configuration.IsHistoricalDefault;
                    hasTmpSettingChanged = true;
                }

                if (ImGui.MenuItem("Today %"))
                {
                    Service.MainWindow.IsOverridingTimeframe = Service.Configuration.IsHistoricalDefault;
                    hasTmpSettingChanged = true;
                }

                ImGui.EndMenu();
            }

            if (hasTmpSettingChanged)
            {
                Service.MainWindow.ResetSize();
                Service.CharDataManager.FetchLogs();
            }

            /*if (!Service.Configuration.IsUpdateDismissed2060)
            {
                var isButtonHidden = !ImGui.IsPopupOpen("##UpdateMessage") && DateTime.Now.Second % 2 == 0;
                if (isButtonHidden)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Vector4.Zero);
                }

                ImGui.PushFont(UiBuilder.IconFont);

                ImGui.SameLine();
                if (ImGui.MenuItem(FontAwesomeIcon.InfoCircle.ToIconString()))
                {
                    ImGui.OpenPopup("##UpdateMessage");
                }

                ImGui.PopFont();

                if (isButtonHidden)
                {
                    ImGui.PopStyleColor();
                }

                if (!Service.Configuration.IsUpdateDismissed2060)
                {
                    Util.SetHoverTooltip("Update message");
                }

                if (ImGui.BeginPopup("##UpdateMessage", ImGuiWindowFlags.NoMove))
                {
                    ImGui.Text(Service.Configuration.IsDefaultLayout
                                   ? "Click an Abyssos encounter/header to swap to Asphodelos and vice versa.\n"
                                   : "A swap group has been added to the default layout to swap from Abyssos to Asphodelos and vice versa.\n" +
                                     "Should you want the same feature, you will have to add the swap yourself in your layout.\n");

                    if (ImGui.Button("Dismiss##UpdateMessage"))
                    {
                        Service.Configuration.IsUpdateDismissed2060 = true;
                        Service.Configuration.Save();
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
            }*/

            ImGui.EndMenuBar();
        }

        ImGui.PopStyleVar();
    }
}
