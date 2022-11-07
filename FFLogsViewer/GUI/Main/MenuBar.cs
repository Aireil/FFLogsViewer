using System;
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
                Service.MainWindow.SetErrorMessage(string.Empty);
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

            ImGui.PushStyleColor(ImGuiCol.Text, Service.MainWindow.Job.Color);
            if (ImGui.BeginMenu(Service.MainWindow.Job.Name))
            {
                foreach (var job in Service.GameDataManager.Jobs)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, job.Color);
                    if (ImGui.MenuItem(job.Name))
                    {
                        Service.MainWindow.Job = job;
                        if (Service.CharDataManager.DisplayedChar.IsInfoSet())
                        {
                            Service.CharDataManager.DisplayedChar.FetchData();
                        }
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.EndMenu();
            }

            ImGui.PopStyleColor();

            if (ImGui.BeginMenu(Service.MainWindow.OverriddenMetric != null
                                    ? Service.MainWindow.OverriddenMetric.Name
                                    : Service.Configuration.Metric.Name))
            {
                foreach (var metric in GameDataManager.AvailableMetrics)
                {
                    if (ImGui.MenuItem(metric.Name))
                    {
                        Service.MainWindow.OverriddenMetric = metric;
                        if (Service.CharDataManager.DisplayedChar.IsInfoSet())
                        {
                            Service.CharDataManager.DisplayedChar.FetchData();
                        }
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Service.MainWindow.Partition.Name))
            {
                foreach (var partition in GameDataManager.AvailablePartitions)
                {
                    if (ImGui.MenuItem(partition.Name))
                    {
                        Service.MainWindow.Partition = partition;
                        if (Service.CharDataManager.DisplayedChar.IsInfoSet())
                        {
                            Service.CharDataManager.DisplayedChar.FetchData();
                        }
                    }
                }

                ImGui.EndMenu();
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
