using Dalamud.Interface.Colors;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class MiscTab
{
    public static void Draw()
    {
        if (ImGui.Button("Open the GitHub repo"))
        {
            Util.OpenLink("https://github.com/Aireil/FFLogsViewer");
        }

        var hasChanged = false;
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Number of decimal digits for logs: ");
        for (var i = 0; i <= 2; i++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton(i + "##NbOfDecimalDigits", Service.Configuration.NbOfDecimalDigits == i))
            {
                Service.Configuration.NbOfDecimalDigits = i;
                hasChanged = true;
            }
        }

        var hideInCombat = Service.Configuration.HideInCombat;
        if (ImGui.Checkbox(@"Hide in combat##HideInCombat", ref hideInCombat))
        {
            Service.Configuration.HideInCombat = hideInCombat;
            hasChanged = true;
        }

        var contextMenu = Service.Configuration.ContextMenu;
        if (ImGui.Checkbox("Enable context menu##ContextMenu", ref contextMenu))
        {
            if (contextMenu)
            {
                ContextMenu.Enable();
            }
            else
            {
                ContextMenu.Disable();
            }

            Service.Configuration.ContextMenu = contextMenu;
            hasChanged = true;
        }

        Util.SetHoverTooltip("Add a button to search characters in most context menus.");

        if (Service.Configuration.ContextMenu)
        {
            ImGui.Indent();
            if (!Service.Configuration.ContextMenuStreamer)
            {
                var contextMenuButtonName = Service.Configuration.ContextMenuButtonName;
                if (ImGui.InputText("Button name##ContextMenuButtonName", ref contextMenuButtonName, 50))
                {
                    Service.Configuration.ContextMenuButtonName = contextMenuButtonName;
                    hasChanged = true;
                }

                var openInBrowser = Service.Configuration.OpenInBrowser;
                if (ImGui.Checkbox(@"Open in browser##OpenInBrowser", ref openInBrowser))
                {
                    Service.Configuration.OpenInBrowser = openInBrowser;
                    hasChanged = true;
                }

                Util.SetHoverTooltip("The button in context menus opens" +
                                     "\nFF Logs in your default browser instead" +
                                     "\nof opening the plugin window.");
            }

            if (!Service.Configuration.OpenInBrowser)
            {
                var contextMenuStreamer = Service.Configuration.ContextMenuStreamer;
                if (ImGui.Checkbox(@"Streamer mode##ContextMenuStreamer", ref contextMenuStreamer))
                {
                    Service.Configuration.ContextMenuStreamer = contextMenuStreamer;
                    hasChanged = true;
                }

                Util.SetHoverTooltip("When the main window is open, opening a context menu" +
                                     "\nwill automatically search for the selected player." +
                                     "\nThis mode does not add a button to the context menu.");
            }

            ImGui.Unindent();
        }

        ImGui.Text("API client:");

        var configurationClientId = Service.Configuration.ClientId;
        if (ImGui.InputText("Client ID##ClientId", ref configurationClientId, 50))
        {
            Service.Configuration.ClientId = configurationClientId;
            Service.FfLogsClient.SetToken();
            hasChanged = true;
        }

        var configurationClientSecret = Service.Configuration.ClientSecret;
        if (ImGui.InputText("Client secret##ClientSecret", ref configurationClientSecret, 50))
        {
            Service.Configuration.ClientSecret = configurationClientSecret;
            Service.FfLogsClient.SetToken();
            hasChanged = true;
        }

        if (Service.FfLogsClient.IsTokenValid)
            ImGui.TextColored(ImGuiColors.HealerGreen, "This client is valid.");
        else
            ImGui.TextColored(ImGuiColors.DalamudRed, "This client is NOT valid.");

        if (ImGui.CollapsingHeader("How to get a client ID and a client secret:"))
        {
            ImGui.Bullet();
            ImGui.Text("Open https://www.fflogs.com/api/clients/ or");
            ImGui.SameLine();
            if (ImGui.Button("Click here##APIClientLink"))
            {
                Util.OpenLink("https://www.fflogs.com/api/clients/");
            }

            ImGui.Bullet();
            ImGui.Text("Create a new client");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7);

            ImGui.Bullet();
            ImGui.Text("Choose any name, for example: \"Plugin\"");
            ImGui.SameLine();
            if (ImGui.Button("Copy##APIClientCopyName"))
            {
                ImGui.SetClipboardText("Plugin");
            }

            ImGui.Bullet();
            ImGui.Text("Enter any URL, for example: \"https://www.example.com\"");
            ImGui.SameLine();
            if (ImGui.Button("Copy##APIClientCopyURL"))
            {
                ImGui.SetClipboardText("https://www.example.com");
            }

            ImGui.Bullet();
            ImGui.Text("Do NOT check the Public Client option");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 7);

            ImGui.Bullet();
            ImGui.Text("Paste both client ID and secret above");
        }

        if (hasChanged)
        {
            Service.Configuration.Save();
        }
    }
}
