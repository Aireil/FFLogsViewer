using Dalamud.Bindings.ImGui;

namespace FFLogsViewer.Model;

public class Style
{
    public float MinMainWindowWidth = 390.0f;
    public ImGuiWindowFlags MainWindowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoScrollbar;
    public ImGuiTableFlags MainTableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerV;
    public bool IsHeaderSeparatorDrawn = true;
    public bool IsSizeFixed;
    public bool IsCloseHotkeyRespected = true;
    public bool AbbreviateJobNames = true;
    public bool IsLocalPlayerInPartyView = true;
}
