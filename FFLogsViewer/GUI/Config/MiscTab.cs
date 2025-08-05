using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;

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

        Util.DrawHelp("Add a button to search characters in most context menus.");

        ImGui.BeginDisabled(!Service.Configuration.ContextMenu);

        ImGui.Indent();

        ImGui.BeginDisabled(Service.Configuration.ContextMenuStreamer);

        var contextMenuButtonName = Service.Configuration.ContextMenuButtonName;
        if (ImGui.InputText("Button name##ContextMenuButtonName", ref contextMenuButtonName, 50))
        {
            Service.Configuration.ContextMenuButtonName = contextMenuButtonName;
            hasChanged = true;
        }

        var openInBrowser = Service.Configuration.OpenInBrowser;
        if (ImGui.Checkbox("Open in browser##OpenInBrowser", ref openInBrowser))
        {
            Service.Configuration.OpenInBrowser = openInBrowser;
            hasChanged = true;
        }

        Util.DrawHelp("The button in context menus opens" +
                             "\nFF Logs in your default browser instead" +
                             "\nof opening the plugin window.");

        ImGui.EndDisabled();

        ImGui.BeginDisabled(Service.Configuration.OpenInBrowser);

        var contextMenuStreamer = Service.Configuration.ContextMenuStreamer;
        if (ImGui.Checkbox("Streamer mode##ContextMenuStreamer", ref contextMenuStreamer))
        {
            Service.Configuration.ContextMenuStreamer = contextMenuStreamer;
            hasChanged = true;
        }

        Util.DrawHelp("When the main window is open, opening a context menu" +
                             "\nwill automatically search for the selected player." +
                             "\nThis mode does not add a button to the context menu.");

        ImGui.BeginDisabled(Service.Configuration.ContextMenuAlwaysPartyView);

        var contextMenuPartyView = Service.Configuration.ContextMenuPartyView;
        if (ImGui.Checkbox("Open the party view when appropriate##ContextMenuPartyView", ref contextMenuPartyView))
        {
            Service.Configuration.ContextMenuPartyView = contextMenuPartyView;
            hasChanged = true;
        }

        Util.DrawHelp("If the context menu button is used from a party list-related window," +
                      "\nopen the party view instead of the single view." +
                      "\nThis will still load the selected player's data in the single view.");

        ImGui.EndDisabled();

        var contextMenuAlwaysPartyView = Service.Configuration.ContextMenuAlwaysPartyView;
        if (ImGui.Checkbox("Always open the party view##ContextMenuAlwaysPartyView", ref contextMenuAlwaysPartyView))
        {
            Service.Configuration.ContextMenuAlwaysPartyView = contextMenuAlwaysPartyView;
            hasChanged = true;
        }

        ImGui.EndDisabled();

        ImGui.Unindent();

        ImGui.EndDisabled();

        var showTomestoneOption = Service.Configuration.ShowTomestoneOption;
        if (ImGui.Checkbox("Show Tomestone option when opening a link", ref showTomestoneOption))
        {
            Service.Configuration.ShowTomestoneOption = showTomestoneOption;
            hasChanged = true;
        }

        var isCachingEnabled = Service.Configuration.IsCachingEnabled;
        if (ImGui.Checkbox("Enable caching", ref isCachingEnabled))
        {
            Service.Configuration.IsCachingEnabled = isCachingEnabled;
            hasChanged = true;
        }

        Util.DrawHelp("Build a cache of fetched characters to avoid using too much API points (see Layout tab for more info on points).\n" +
                      "The cache is cleared every hour, you can also manually clear it in the main window.");

        ImGui.Text("API client:");

        var configurationClientId = Service.Configuration.ClientId;
        if (ImGui.InputText("Client ID##ClientId", ref configurationClientId, 50))
        {
            Service.Configuration.ClientId = configurationClientId;
            Service.FFLogsClient.SetToken();
            hasChanged = true;
        }

        var configurationClientSecret = Service.Configuration.ClientSecret;
        if (ImGui.InputText("Client secret##ClientSecret", ref configurationClientSecret, 50))
        {
            Service.Configuration.ClientSecret = configurationClientSecret;
            Service.FFLogsClient.SetToken();
            hasChanged = true;
        }

        if (Service.FFLogsClient.IsTokenValid)
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, "This client is valid.");
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "This client is NOT valid.");
            if (FFLogsClient.IsConfigSet())
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGui.TextWrapped("If you are certain that the API client is valid, this may indicate that FF Logs is unreachable.\nMake sure you can open it in your browser before trying again.");
                color.Pop();
                if (ImGui.Button("Open FF Logs"))
                {
                    Util.OpenLink("https://www.fflogs.com/");
                }

                ImGui.SameLine();
                if (ImGui.Button("Try again"))
                {
                    Service.FFLogsClient.SetToken();
                }
            }
        }

        if (ImGui.CollapsingHeader("How to get a client ID and a client secret:"))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text("Open https://www.fflogs.com/api/clients/ or");
            ImGui.SameLine();
            if (ImGui.Button("Click here##APIClientLink"))
            {
                Util.OpenLink("https://www.fflogs.com/api/clients/");
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text("Create a new client");

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text("Choose any name, for example: \"Plugin\"");
            ImGui.SameLine();
            if (ImGui.Button("Copy##APIClientCopyName"))
            {
                CopyToClipboard("Plugin");
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text("Enter any URL, for example: \"https://www.example.com\"");
            ImGui.SameLine();
            if (ImGui.Button("Copy##APIClientCopyURL"))
            {
                CopyToClipboard("https://www.example.com");
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text("Do NOT check the Public Client option");

            ImGui.AlignTextToFramePadding();
            ImGui.Bullet();
            ImGui.Text("Paste both client ID and secret above");
        }

        if (hasChanged)
        {
            Service.Configuration.Save();
        }
    }

    private static void CopyToClipboard(string text)
    {
        try
        {
            ImGui.SetClipboardText(text);
            Service.NotificationManager.AddNotification(new Notification { Content = $"Copied to clipboard: {text}", Type = NotificationType.Success });
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Could not set clipboard text.");
            Service.NotificationManager.AddNotification(new Notification { Title = "Could not copy to clipboard", Content = text, Type = NotificationType.Error, Minimized = false });
        }
    }
}
