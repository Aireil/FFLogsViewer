using System;
using System.Linq;
using System.Numerics;
using FFLogsViewer.Model;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class Table
{
    public static void Draw()
    {
        var enabledStats = Service.Configuration.Stats.Where(stat => stat.IsEnabled).ToList();
        if (ImGui.BeginTable(
                    "##MainWindowTable",
                    enabledStats.Count + 1,
                    Service.Configuration.Style.MainTableFlags))
        {
            foreach (var entry in Service.Configuration.Layout)
            {
                ImGui.TableNextColumn();

                if (entry.Type == LayoutEntryType.Header)
                {
                    var separatorCursorY = 0.0f;
                    if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
                    {
                        ImGui.Separator();
                        separatorCursorY = ImGui.GetCursorPosY();
                    }

                    ImGui.Text(entry.Alias);

                    foreach (var stat in enabledStats)
                    {
                        ImGui.TableNextColumn();
                        if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
                        {
                            ImGui.SetCursorPosY(separatorCursorY);
                        }

                        if (stat.Type == StatType.BestAmount &&
                            stat.Alias.Equals("/metric/", StringComparison.OrdinalIgnoreCase))
                        {
                            var metricName = Service.CharDataManager.DisplayedChar.LoadedMetric != null
                                                 ? Service.CharDataManager.DisplayedChar.LoadedMetric.Name
                                                 : Service.Configuration.Metric.Name;
                            metricName = metricName.Replace("Healer Combined", "HC");
                            metricName = metricName.Replace("Tank Combined", "TC");
                            Util.CenterText(metricName);
                        }
                        else
                        {
                            Util.CenterText(stat.Alias != string.Empty ? stat.Alias : stat.Name);
                        }
                    }

                    if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
                    {
                        ImGui.Separator();
                    }
                }
                else if (entry.Type == LayoutEntryType.Encounter)
                {
                    ImGui.Text(entry.Alias != string.Empty ? entry.Alias : entry.Encounter);

                    var encounter =
                        Service.CharDataManager.DisplayedChar.Encounters.FirstOrDefault(
                            enc => enc.Id == entry.EncounterId && enc.Difficulty == entry.DifficultyId);
                    foreach (var stat in enabledStats)
                    {
                        ImGui.TableNextColumn();
                        string? text = null;
                        Vector4? color = null;
                        switch (stat.Type)
                        {
                            case StatType.Best:
                                text = encounter?.Best?.ToString();
                                color = Util.GetLogColor(encounter?.Best);
                                break;
                            case StatType.Median:
                                text = encounter?.Median?.ToString();
                                color = Util.GetLogColor(encounter?.Median);
                                break;
                            case StatType.Kills:
                                text = encounter?.Kills?.ToString();
                                break;
                            case StatType.Fastest:
                                if (encounter?.Fastest != null)
                                {
                                    text = TimeSpan.FromMilliseconds(encounter.Fastest.Value).ToString("mm':'ss");
                                }

                                break;
                            case StatType.BestAmount:
                                text = encounter?.BestAmount?.ToString();
                                break;
                            case StatType.Job:
                                text = encounter?.Job?.Name;
                                color = encounter?.Job?.Color;
                                break;
                            case StatType.BestJob:
                                text = encounter?.BestJob?.Name;
                                color = encounter?.BestJob?.Color;
                                break;
                            default:
                                text = "?";
                                break;
                        }

                        text ??= "-";
                        color ??= new Vector4(1, 1, 1, 1);

                        Util.CenterTextColored(color.Value, text);
                    }
                }

                ImGui.TableNextRow();
            }

            ImGui.EndTable();
        }
    }
}
