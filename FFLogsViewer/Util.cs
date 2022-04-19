using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using FFLogsViewer.Manager;
using ImGuiNET;

namespace FFLogsViewer;

public class Util
{
    public static bool DrawButtonIcon(FontAwesomeIcon icon, Vector2? size = null)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        if (size != null)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, size.Value);
        }

        var ret = ImGui.Button(icon.ToIconString());

        if (size != null)
        {
            ImGui.PopStyleVar();
        }

        ImGui.PopFont();

        return ret;
    }

    public static bool DrawDisabledButton(string label, bool isDisabled)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, isDisabled ? 0.5f : 1.0f);
        var ret = ImGui.Button(label);
        ImGui.PopStyleVar();

        return ret;
    }

    public static void DrawHelp(string helpMessage)
    {
        ImGui.SameLine();
        ImGui.TextColored(ImGuiColors.DalamudGrey, "(?)");

        SetHoverTooltip(helpMessage);
    }

    public static void IncList<T>(List<T> list, int index)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));
        var indexA = Mod(index, list.Count);
        var indexB = Mod(index - 1, list.Count);
        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
    }

    public static void DecList<T>(List<T> list, int index)
    {
        var indexA = Mod(index, list.Count);
        var indexB = Mod(index + 1, list.Count);
        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
    }

    public static Vector4 GetLogColor(int? log)
    {
        var color = log switch
        {
            < 0 => new Vector4(255, 255, 255, 255),
            < 25 => new Vector4(102, 102, 102, 255),
            < 50 => new Vector4(30, 255, 0, 255),
            < 75 => new Vector4(0, 112, 255, 255),
            < 95 => new Vector4(163, 53, 238, 255),
            < 99 => new Vector4(255, 128, 0, 255),
            99 => new Vector4(226, 104, 168, 255),
            100 => new Vector4(229, 204, 128, 255),
            _ => new Vector4(255, 255, 255, 255),
        };

        return color / 255;
    }

    public static void CenterCursor(string text)
    {
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
    }

    public static void CenterTextColored(Vector4 color, string text)
    {
        CenterCursor(text);
        ImGui.TextColored(color, text);
    }

    public static void CenterText(string text)
    {
        CenterCursor(text);
        ImGui.Text(text);
    }

    public static void CenterSelectable(string text, ref bool isClicked)
    {
        CenterCursor(text);
        var textSize = ImGui.CalcTextSize(text);
        ImGui.Selectable(text, ref isClicked, ImGuiSelectableFlags.None, textSize);
    }

    public static void SetHoverTooltip(string tooltip)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }

    public static int Mod(int x, int m) => ((x % m) + m) % m;

    public static void OpenLink(CharData charData)
    {
        OpenLink($"https://fflogs.com/character/{CharDataManager.GetRegionName(charData.WorldName)}/{charData.WorldName}/{charData.FirstName} {charData.LastName}");
    }

    public static void OpenLink(string link)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = link,
            UseShellExecute = true,
        });
    }

    public static unsafe SeString ReadSeString(byte* ptr)
    {
        var offset = 0;
        while (true)
        {
            var b = *(ptr + offset);
            if (b == 0)
            {
                break;
            }

            offset += 1;
        }

        var bytes = new byte[offset];
        Marshal.Copy(new IntPtr(ptr), bytes, 0, offset);
        return SeString.Parse(bytes);
    }
}
