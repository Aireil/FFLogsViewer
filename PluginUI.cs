using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;

namespace FFLogsViewer
{
    internal class PluginUi : IDisposable
    {
        private readonly string[] _characterInput = new string[3];
        private readonly Vector4 _defaultColor = new(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Dictionary<string, Vector4> _jobColors = new();
        private readonly Dictionary<string, Vector4> _logColors = new();
        private readonly FFLogsViewer _plugin;

        private float _bossesColumnWidth;

        private string _errorMessage = "";

        private bool _hasLoadingFailed;

        private bool _isLinkClicked;
        private bool _isRaidButtonClicked;
        private bool _isUltimateButtonClicked;
        private bool _isConfigClicked;
        private bool _isSpoilerClicked;
        private float _jobsColumnWidth;
        private float _logsColumnWidth;
        private CharacterData _selectedCharacterData = new();
        private bool _settingsVisible;

        private bool _visible;

        internal PluginUi(FFLogsViewer plugin)
        {
            this._plugin = plugin;

            this._jobColors.Add("Astrologian", new Vector4(255.0f / 255.0f, 231.0f / 255.0f, 74.0f / 255.0f, 1.0f));
            this._jobColors.Add("Bard", new Vector4(145.0f / 255.0f, 150.0f / 255.0f, 186.0f / 255.0f, 1.0f));
            this._jobColors.Add("Black Mage", new Vector4(165.0f / 255.0f, 121.0f / 255.0f, 214.0f / 255.0f, 1.0f));
            this._jobColors.Add("Dancer", new Vector4(226.0f / 255.0f, 176.0f / 255.0f, 175.0f / 255.0f, 1.0f));
            this._jobColors.Add("Dark Knight", new Vector4(209.0f / 255.0f, 38.0f / 255.0f, 204.0f / 255.0f, 1.0f));
            this._jobColors.Add("Dragoon", new Vector4(65.0f / 255.0f, 100.0f / 255.0f, 205.0f / 255.0f, 1.0f));
            this._jobColors.Add("Gunbreaker", new Vector4(121.0f / 255.0f, 109.0f / 255.0f, 48.0f / 255.0f, 1.0f));
            this._jobColors.Add("Machinist", new Vector4(110.0f / 255.0f, 225.0f / 255.0f, 214.0f / 255.0f, 1.0f));
            this._jobColors.Add("Monk", new Vector4(214.0f / 255.0f, 156.0f / 255.0f, 0.0f / 255.0f, 1.0f));
            this._jobColors.Add("Ninja", new Vector4(175.0f / 255.0f, 25.0f / 255.0f, 100.0f / 255.0f, 1.0f));
            this._jobColors.Add("Paladin", new Vector4(168.0f / 255.0f, 210.0f / 255.0f, 230.0f / 255.0f, 1.0f));
            this._jobColors.Add("Red Mage", new Vector4(232.0f / 255.0f, 123.0f / 255.0f, 123.0f / 255.0f, 1.0f));
            this._jobColors.Add("Samurai", new Vector4(228.0f / 255.0f, 109.0f / 255.0f, 4.0f / 255.0f, 1.0f));
            this._jobColors.Add("Scholar", new Vector4(134.0f / 255.0f, 87.0f / 255.0f, 255.0f / 255.0f, 1.0f));
            this._jobColors.Add("Summoner", new Vector4(45.0f / 255.0f, 155.0f / 255.0f, 120.0f / 255.0f, 1.0f));
            this._jobColors.Add("Warrior", new Vector4(207.0f / 255.0f, 38.0f / 255.0f, 33.0f / 255.0f, 1.0f));
            this._jobColors.Add("White Mage", new Vector4(255.0f / 255.0f, 240.0f / 255.0f, 220.0f / 255.0f, 1.0f));
            this._jobColors.Add("Default", this._defaultColor);

            this._logColors.Add("Grey", new Vector4(102.0f / 255.0f, 102.0f / 255.0f, 102.0f / 255.0f, 1.0f));
            this._logColors.Add("Green", new Vector4(30.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f, 1.0f));
            this._logColors.Add("Blue", new Vector4(0.0f / 255.0f, 112.0f / 255.0f, 255.0f / 255.0f, 1.0f));
            this._logColors.Add("Magenta", new Vector4(163.0f / 255.0f, 53.0f / 255.0f, 238.0f / 255.0f, 1.0f));
            this._logColors.Add("Orange", new Vector4(255.0f / 255.0f, 128.0f / 255.0f, 0.0f / 255.0f, 1.0f));
            this._logColors.Add("Pink", new Vector4(226.0f / 255.0f, 104.0f / 255.0f, 168.0f / 255.0f, 1.0f));
            this._logColors.Add("Yellow", new Vector4(229.0f / 255.0f, 204.0f / 255.0f, 128.0f / 255.0f, 1.0f));
            this._logColors.Add("Default", this._defaultColor);
        }

        internal bool Visible
        {
            get => this._visible;
            set => this._visible = value;
        }

        internal bool SettingsVisible
        {
            get => this._settingsVisible;
            set => this._settingsVisible = value;
        }

        public void Dispose()
        {
        }

        internal void Draw()
        {
            DrawSettingsWindow();
            DrawMainWindow();
        }

        private void DrawSettingsWindow()
        {
            if (!this.SettingsVisible) return;

            ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
            if (ImGui.Begin("FF Logs Viewer Config", ref this._settingsVisible,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize))
            {
                var showSpoilers = this._plugin.Configuration.ShowSpoilers;
                if (ImGui.Checkbox("Show spoilers##ShowSpoilers", ref showSpoilers))
                {
                    this._plugin.ToggleContextMenuButton(showSpoilers);
                    this._plugin.Configuration.ShowSpoilers = showSpoilers;
                    this._plugin.Configuration.Save();
                }

                // TODO common fix
                if (this._plugin.isCommonBroken)
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Context menu button is disabled in 6.1, waiting for a fix.");
                }

                var contextMenu = this._plugin.Configuration.ContextMenu;
                if (ImGui.Checkbox("Search button in context menus##ContextMenu", ref contextMenu))
                {
                    this._plugin.ToggleContextMenuButton(contextMenu);
                    this._plugin.Configuration.ContextMenu = contextMenu;
                    this._plugin.Configuration.Save();
                }

                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add a button to search characters in most context menus.");

                if (this._plugin.Configuration.ContextMenu)
                {
                    var openInBrowser = this._plugin.Configuration.OpenInBrowser;
                    if (ImGui.Checkbox(@"Open in browser##OpenInBrowser", ref openInBrowser))
                    {
                        this._plugin.Configuration.OpenInBrowser = openInBrowser;
                        this._plugin.Configuration.Save();
                    }

                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("The button in context menus opens" +
                                                                "\nFFLogs in your default browser instead" +
                                                                "\nof opening the plugin window.");

                    if (!this._plugin.Configuration.ContextMenuStreamer)
                    {
                        var contextMenuButtonName = this._plugin.Configuration.ContextMenuButtonName ?? string.Empty;
                        if (ImGui.InputText("Button name##ContextMenuButtonName", ref contextMenuButtonName, 50))
                        {
                            this._plugin.Configuration.ContextMenuButtonName = contextMenuButtonName;
                            this._plugin.Configuration.Save();
                        }

                    }

                    var contextMenuStreamer = this._plugin.Configuration.ContextMenuStreamer;
                    if (ImGui.Checkbox(@"Streamer mode##ContextMenuStreamer", ref contextMenuStreamer))
                    {
                        this._plugin.Configuration.ContextMenuStreamer = contextMenuStreamer;
                        this._plugin.Configuration.Save();
                    }

                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("When the FF Logs Viewer window is open, opening a context menu" +
                                                                "\nwill automatically search for the selected player." +
                                                                "\nThis mode does not add a button to the context menu.");

                    var hideInCombat = this._plugin.Configuration.HideInCombat;
                    if (ImGui.Checkbox(@"Hide in combat##HideInCombat", ref hideInCombat))
                    {
                        this._plugin.Configuration.HideInCombat = hideInCombat;
                        this._plugin.Configuration.Save();
                    }
                }

                ImGui.Text("API client:");

                var configurationClientId = this._plugin.Configuration.ClientId ?? string.Empty;
                if (ImGui.InputText("Client ID##ClientId", ref configurationClientId, 50))
                {
                    this._plugin.Configuration.ClientId = configurationClientId;
                    this._plugin.FfLogsClient.SetToken();
                    this._plugin.Configuration.Save();
                }

                var configurationClientSecret = this._plugin.Configuration.ClientSecret ?? string.Empty;
                if (ImGui.InputText("Client secret##ClientSecret", ref configurationClientSecret, 50))
                {
                    this._plugin.Configuration.ClientSecret = configurationClientSecret;
                    this._plugin.FfLogsClient.SetToken();
                    this._plugin.Configuration.Save();
                }

                if (this._plugin.FfLogsClient.IsTokenValid)
                    ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "This client is valid.");
                else
                    ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "This client is NOT valid.");

                ImGui.Text("How to get a client ID and a client secret:");

                ImGui.Bullet();
                ImGui.Text($"Open https://{this._plugin.FflogsHost}/api/clients/ or");
                ImGui.SameLine();
                if (ImGui.Button("Click here##APIClientLink"))
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = $"https://{this._plugin.FflogsHost}/api/clients/",
                        UseShellExecute = true,
                    });
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

            ImGui.End();
        }

        private void DrawMainWindow()
        {
            if (!this.Visible
                || (this._plugin.Configuration.HideInCombat && DalamudApi.Condition[ConditionFlag.InCombat]))
                return;

            var windowHeight = 293 * ImGui.GetIO().FontGlobalScale + 100;
            var reducedWindowHeight = 58 * ImGui.GetIO().FontGlobalScale + 30;
            var windowWidth = 407 * ImGui.GetIO().FontGlobalScale;

            ImGui.SetNextWindowSize(new Vector2(windowWidth, reducedWindowHeight), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("FF Logs Viewer", ref this._visible,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Columns(this._plugin.IsChinese ? 3 : 4, "InputColumns", true);

                var buttonsWidth = ((ImGui.CalcTextSize("Target") + ImGui.CalcTextSize("Clipboard")).X + (40.0f * ImGui.GetIO().FontGlobalScale));
                var sizeMin = Math.Max(ImGui.CalcTextSize(this._selectedCharacterData.FirstName).X,
                    Math.Max(ImGui.CalcTextSize(this._selectedCharacterData.LastName).X,
                        ImGui.CalcTextSize(this._selectedCharacterData.WorldName).X));
                var idealWindowWidth = sizeMin * (this._plugin.IsChinese ? 2 : 3) + buttonsWidth + 73.0f;
                if (idealWindowWidth < windowWidth) idealWindowWidth = windowWidth;
                float idealWindowHeight;
                if (this._selectedCharacterData.IsEveryLogsReady && !this._hasLoadingFailed)
                    idealWindowHeight = windowHeight;
                else
                    idealWindowHeight = reducedWindowHeight;
                var colWidth = ((idealWindowWidth - buttonsWidth) / (this._plugin.IsChinese ? 2.0f : 3.0f));
                ImGui.SetWindowSize(new Vector2(idealWindowWidth, idealWindowHeight));

                if (this._plugin.IsChinese)
                {
                    ImGui.SetColumnWidth(0, colWidth);
                    ImGui.SetColumnWidth(1, colWidth);
                    ImGui.SetColumnWidth(2, buttonsWidth);
                }
                else
                {
                    ImGui.SetColumnWidth(0, colWidth);
                    ImGui.SetColumnWidth(1, colWidth);
                    ImGui.SetColumnWidth(2, colWidth);
                    ImGui.SetColumnWidth(3, buttonsWidth);
                }


                ImGui.PushItemWidth(colWidth - 15);
                this._characterInput[0] = this._selectedCharacterData.FirstName;
                ImGui.InputTextWithHint("##FirstName", "First Name", ref this._characterInput[0], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                this._selectedCharacterData.FirstName = this._characterInput[0];
                ImGui.PopItemWidth();

                if (!this._plugin.IsChinese)
                {
                    ImGui.NextColumn();
                    ImGui.PushItemWidth(colWidth - 15);
                    this._characterInput[1] = this._selectedCharacterData.LastName;
                    ImGui.InputTextWithHint("##LastName", "Last Name", ref this._characterInput[1], 256,
                        ImGuiInputTextFlags.CharsNoBlank);
                    this._selectedCharacterData.LastName = this._characterInput[1];
                    ImGui.PopItemWidth();
                }

                ImGui.NextColumn();
                ImGui.PushItemWidth(colWidth - 14);
                this._characterInput[2] = this._selectedCharacterData.WorldName;
                ImGui.InputTextWithHint("##WorldName", "World Name", ref this._characterInput[2], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                this._selectedCharacterData.WorldName = this._characterInput[2];

                ImGui.PopItemWidth();

                ImGui.NextColumn();
                if (ImGui.Button("Clipboard"))
                    try
                    {
                        this._selectedCharacterData = this._plugin.GetClipboardCharacter();
                        this._errorMessage = "";
                        this._hasLoadingFailed = false;
                        try
                        {
                            this._plugin.FetchLogs(this._selectedCharacterData);
                        }
                        catch
                        {
                            this._errorMessage = "World not supported or invalid.";
                        }
                    }
                    catch
                    {
                        this._errorMessage = "No character found in the clipboard.";
                    }

                ImGui.SameLine();
                if (ImGui.Button("Target"))
                    try
                    {
                        this._selectedCharacterData = this._plugin.GetTargetCharacter();
                        this._errorMessage = "";
                        this._hasLoadingFailed = false;
                        try
                        {
                            this._plugin.FetchLogs(this._selectedCharacterData);
                        }
                        catch
                        {
                            this._errorMessage = "World not supported or invalid.";
                        }
                    }
                    catch
                    {
                        this._errorMessage = "Invalid target.";
                    }

                ImGui.Columns();

                ImGui.Separator();

                if (ImGui.Button("Clear"))
                {
                    this._selectedCharacterData = new CharacterData();
                    this._errorMessage = "";
                    this._hasLoadingFailed = false;
                }

                ImGui.SameLine();
                if (!this._plugin.FfLogsClient.IsTokenValid)
                {
                    var message = !this._plugin.IsConfigSetup() ? "Config not setup, click to open settings." : "API client not valid, click to open settings.";
                    var messageSize = ImGui.CalcTextSize(message);
                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - messageSize.X / 2);
                    messageSize.X -= 7; // A bit too large on right side
                    messageSize.Y += 1;
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    ImGui.Selectable(
                        message,
                        ref this._isConfigClicked, ImGuiSelectableFlags.None, messageSize);
                    ImGui.PopStyleColor();

                    if (this._isConfigClicked)
                    {
                        this.SettingsVisible = true;
                        this._isConfigClicked = false;
                    }
                }
                else
                {
                    if (!this._plugin.IsChinese || this._plugin.Configuration.hasDismissed)
                    {
                        if (this._errorMessage == "")
                        {
                            if (this._selectedCharacterData.IsEveryLogsReady)
                            {
                                var message = $"Viewing {this._selectedCharacterData.LoadedFirstName} {this._selectedCharacterData.LoadedLastName}@{this._selectedCharacterData.LoadedWorldName}'s logs.";
                                var messageSize = ImGui.CalcTextSize(message);
                                ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - messageSize.X / 2);
                                messageSize.X -= (7 * ImGui.GetIO().FontGlobalScale); // A bit too large on right side
                                messageSize.Y += (1 * ImGui.GetIO().FontGlobalScale);
                                ImGui.Selectable(
                                    message,
                                    ref this._isLinkClicked, ImGuiSelectableFlags.None, messageSize);

                                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to open on FF Logs.");

                                if (this._isLinkClicked)
                                {
                                    Process.Start(new ProcessStartInfo()
                                    {
                                        FileName = $"https://{this._plugin.FflogsHost}/character/{this._selectedCharacterData.RegionName}/{this._selectedCharacterData.WorldName}/{this._selectedCharacterData.FirstName} {this._selectedCharacterData.LastName}",
                                        UseShellExecute = true,
                                    });
                                    this._isLinkClicked = false;
                                }
                            }
                            else if (this._selectedCharacterData.IsDataLoading)
                            {
                                ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize("Loading...").X / 2);
                                ImGui.TextUnformatted("Loading...");
                            }
                            else
                            {
                                ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize("Waiting...").X / 2);
                                ImGui.TextUnformatted("Waiting...");
                            }
                        }
                        else
                        {
                            ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize(this._errorMessage).X / 2);
                            ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), this._errorMessage);
                        }
                    }
                    else
                    {
                        var message = "CN support is ending, hover for more info.";
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - ImGui.CalcTextSize(message).X / 2);
                        if (ImGui.Button(message))
                        {
                            this._plugin.Configuration.hasDismissed = true;
                            this._plugin.Configuration.Save();
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to dismiss the button. Once the full update for the global patch 6.1 is released, this version of the plugin will no longer support the Chinese Client.\nPlease look in issues on the GitHub, I am hoping someone can maintain a version in a fork, but it will cause too much trouble having both global and CN in the same repo.\nHope you can understand, I'm sorry about that.");
                    }
                }


                ImGui.SameLine();

                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize("Search").X * 1.5f + 4.0f);

                if (ImGui.Button("Search"))
                {
                    if (this._selectedCharacterData.IsCharacterReady(this._plugin.IsChinese))
                    {
                        this._errorMessage = "";
                        try
                        {
                            this._plugin.FetchLogs(this._selectedCharacterData);
                        }
                        catch
                        {
                            this._errorMessage = "World not supported or invalid.";
                        }

                        this._hasLoadingFailed = false;
                    }
                    else
                    {
                        this._errorMessage = "One of the inputs is empty.";
                    }
                }

                if (this._selectedCharacterData.IsEveryLogsReady && !this._hasLoadingFailed)
                {
                    ImGui.Separator();

                    ImGui.Columns(5, "LogsDisplay", true);

                    this._bossesColumnWidth =
                        ImGui.CalcTextSize("Cloud of Darkness").X + 17.5f; // Biggest text in first column
                    this._jobsColumnWidth = ImGui.CalcTextSize("Dark Knight").X + 17.5f; // Biggest job name
                    this._logsColumnWidth = (ImGui.GetWindowWidth() - this._bossesColumnWidth - this._jobsColumnWidth) /
                                            3.0f;
                    ImGui.SetColumnWidth(0, this._bossesColumnWidth);
                    ImGui.SetColumnWidth(1, this._logsColumnWidth);
                    ImGui.SetColumnWidth(2, this._logsColumnWidth);
                    ImGui.SetColumnWidth(3, this._logsColumnWidth);
                    ImGui.SetColumnWidth(4, this._jobsColumnWidth);

                    var raidName = this._plugin.Configuration.DisplayOldRaid ? "Eden's Promise (?)" : "Asphodelos (?)";
                    ImGui.SetCursorPosX(8.0f);
                    ImGui.Selectable(
                        raidName,
                        ref this._isRaidButtonClicked, ImGuiSelectableFlags.None);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to switch to " + (this._plugin.Configuration.DisplayOldRaid ? "Asphodelos." : "Eden's Promise."));

                    if (this._isRaidButtonClicked)
                    {
                        this._plugin.Configuration.DisplayOldRaid = !this._plugin.Configuration.DisplayOldRaid;
                        this._plugin.Configuration.Save();
                        this._isRaidButtonClicked = false;
                    }

                    ImGui.Spacing();
                    if (this._plugin.Configuration.DisplayOldRaid)
                    {
                        PrintTextColumn(1, "Cloud of Darkness");
                        PrintTextColumn(1, "Shadowkeeper");
                        PrintTextColumn(1, "Fatebreaker");
                        PrintTextColumn(1, "Eden's Promise");
                        PrintTextColumn(1, "Oracle of Darkness");
                    }
                    else
                    {
                        PrintTextColumn(1, "Erichthonios");
                        PrintTextColumn(1, "Hippokampos");
                        PrintTextColumn(1, "Phoinix");
                        PrintTextColumn(1, "Hesperos");
                        PrintTextColumn(1, "Hesperos II");
                    }
                    ImGui.Spacing();
                    var ultimateName = this._plugin.Configuration.DisplayOldUltimate ? "Ultimates (ShB) (?)" : "Ultimates (EW) (?)";
                    ImGui.SetCursorPosX(8.0f);
                    ImGui.Selectable(
                        ultimateName,
                        ref this._isUltimateButtonClicked, ImGuiSelectableFlags.None);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to switch to " + (this._plugin.Configuration.DisplayOldUltimate ? "Endwalker ultimates." : "Shadowbringers ultimates."));

                    if (this._isUltimateButtonClicked)
                    {
                        this._plugin.Configuration.DisplayOldUltimate = !this._plugin.Configuration.DisplayOldUltimate;
                        this._plugin.Configuration.Save();
                        this._isUltimateButtonClicked = false;
                    }
                    ImGui.Spacing();
                    PrintTextColumn(1, "UCoB");
                    PrintTextColumn(1, "UwU");
                    PrintTextColumn(1, "TEA");

                    ImGui.Spacing();
                    PrintTextColumn(1, "Trials (Extreme)");
                    ImGui.Spacing();
                    if (this._plugin.Configuration.ShowSpoilers)
                    {
                        PrintTextColumn(1, "Zodiark");
                        PrintTextColumn(1, "Hydaelyn");
                    }
                    else
                    {
                        ImGui.SetCursorPosX(8.0f);
                        ImGui.Selectable(
                            "Trial 1 (?)",
                            ref this._isSpoilerClicked, ImGuiSelectableFlags.None);
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Big MSQ spoiler, click to display.");

                        ImGui.SetCursorPosX(8.0f);
                        ImGui.Selectable(
                            "Trial 2 (?)",
                            ref this._isSpoilerClicked, ImGuiSelectableFlags.None);
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Big MSQ spoiler, click to display.");

                        if (this._isSpoilerClicked)
                        {
                            this._plugin.Configuration.ShowSpoilers = !this._plugin.Configuration.ShowSpoilers;
                            this._plugin.Configuration.Save();
                            this._isSpoilerClicked = false;
                        }
                    }
                    PrintTextColumn(1, "Endsinger");

                    try
                    {
                        ImGui.NextColumn();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.CloudOfDarkness : CharacterData.BossesId.Erichthonios, CharacterData.DataType.Best, 2);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Shadowkeeper : CharacterData.BossesId.Hippokampos, CharacterData.DataType.Best, 2);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Fatebreaker : CharacterData.BossesId.Phoinix, CharacterData.DataType.Best, 2);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.EdensPromise : CharacterData.BossesId.Hesperos, CharacterData.DataType.Best, 2);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.OracleOfDarkness : CharacterData.BossesId.HesperosII, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UCoBShB : CharacterData.BossesId.UCoB, CharacterData.DataType.Best, 2);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UwUShB : CharacterData.BossesId.UwU, CharacterData.DataType.Best, 2);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.TeaShB : CharacterData.BossesId.Tea, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Zodiark, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.Hydaelyn, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.Endsinger, CharacterData.DataType.Best, 2);

                        ImGui.NextColumn();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.CloudOfDarkness : CharacterData.BossesId.Erichthonios, CharacterData.DataType.Median, 3);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Shadowkeeper : CharacterData.BossesId.Hippokampos, CharacterData.DataType.Median, 3);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Fatebreaker : CharacterData.BossesId.Phoinix, CharacterData.DataType.Median, 3);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.EdensPromise : CharacterData.BossesId.Hesperos, CharacterData.DataType.Median, 3);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.OracleOfDarkness : CharacterData.BossesId.HesperosII, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UCoBShB : CharacterData.BossesId.UCoB, CharacterData.DataType.Median, 3);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UwUShB : CharacterData.BossesId.UwU, CharacterData.DataType.Median, 3);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.TeaShB : CharacterData.BossesId.Tea, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Zodiark, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.Hydaelyn, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.Endsinger, CharacterData.DataType.Median, 3);

                        ImGui.NextColumn();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.CloudOfDarkness : CharacterData.BossesId.Erichthonios, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Shadowkeeper : CharacterData.BossesId.Hippokampos, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Fatebreaker : CharacterData.BossesId.Phoinix, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.EdensPromise : CharacterData.BossesId.Hesperos, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.OracleOfDarkness : CharacterData.BossesId.HesperosII, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UCoBShB : CharacterData.BossesId.UCoB, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UwUShB : CharacterData.BossesId.UwU, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.TeaShB : CharacterData.BossesId.Tea, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Zodiark, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.Hydaelyn, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.Endsinger, CharacterData.DataType.Kills, 4);

                        ImGui.NextColumn();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.CloudOfDarkness : CharacterData.BossesId.Erichthonios, CharacterData.DataType.Job, 5);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Shadowkeeper : CharacterData.BossesId.Hippokampos, CharacterData.DataType.Job, 5);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.Fatebreaker : CharacterData.BossesId.Phoinix, CharacterData.DataType.Job, 5);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.EdensPromise : CharacterData.BossesId.Hesperos, CharacterData.DataType.Job, 5);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldRaid ? CharacterData.BossesId.OracleOfDarkness : CharacterData.BossesId.HesperosII, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UCoBShB : CharacterData.BossesId.UCoB, CharacterData.DataType.Job, 5);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.UwUShB : CharacterData.BossesId.UwU, CharacterData.DataType.Job, 5);
                        PrintDataColumn(this._plugin.Configuration.DisplayOldUltimate ? CharacterData.BossesId.TeaShB : CharacterData.BossesId.Tea, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.Zodiark, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.Hydaelyn, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.Endsinger, CharacterData.DataType.Job, 5);

                        ImGui.Columns();
                    }
                    catch (Exception e)
                    {
                        this._errorMessage = "Logs could not be loaded.";
                        PluginLog.LogError(e, "Logs could not be loaded.");
                        this._hasLoadingFailed = true;
                    }
                }
            }

            ImGui.End();
        }

        private void PrintDataColumn(CharacterData.BossesId bossId, CharacterData.DataType dataType, int column)
        {
            string text = null;
            var color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            int log;
            switch (dataType)
            {
                case CharacterData.DataType.Best:
                    if (this._selectedCharacterData.Bests.TryGetValue((int)bossId, out log))
                    {
                        text = log == -1 ? "-" : log.ToString();
                        color = GetLogColor(log);
                    }

                    break;

                case CharacterData.DataType.Median:
                    if (this._selectedCharacterData.Medians.TryGetValue((int)bossId, out log))
                    {
                        text = log == -1 ? "-" : log.ToString();
                        color = GetLogColor(log);
                    }

                    break;

                case CharacterData.DataType.Kills:
                    if (this._selectedCharacterData.Kills.TryGetValue((int)bossId, out log))
                    {
                        text = log == -1 ? "-" : log.ToString();
                    }

                    break;

                case CharacterData.DataType.Job:
                    if (this._selectedCharacterData.Jobs.TryGetValue((int)bossId, out var job))
                    {
                        text = job;
                        if (!this._jobColors.TryGetValue(job, out color))
                        {
                            color = this._jobColors["Default"];
                        }
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            if (text != null)
            {
                var isLockedIn = true;
                if (this._selectedCharacterData.IsLockedInDic.ContainsKey((int)bossId))
                {
                    isLockedIn = this._selectedCharacterData.IsLockedInDic[(int)bossId];
                }
                if (dataType is CharacterData.DataType.Best or CharacterData.DataType.Median &&
                    !isLockedIn)
                {
                    text += " (?)";
                }
                PrintTextColumn(column, text, color);
                if (dataType is CharacterData.DataType.Best or CharacterData.DataType.Median && !isLockedIn)
                {
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip("Not locked in.");
                }
            }
            else
            {
                PrintTextColumn(column, "N/A", color);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Data not available. This is expected if the boss is not yet on FF Logs.");
            }

        }

        private void PrintTextColumn(int column, string text, Vector4 color)
        {
            var position = column switch
            {
                1 => 8.0f,
                2 or 3 or 4 => this._bossesColumnWidth + this._logsColumnWidth / 2.0f +
                               this._logsColumnWidth * (column - 2) -
                               ImGui.CalcTextSize(text).X / 2.0f,
                _ => this._bossesColumnWidth + this._jobsColumnWidth / 2.0f + this._logsColumnWidth * (column - 2) -
                     ImGui.CalcTextSize(text).X / 2.0f,
            };

            ImGui.SetCursorPosX(position);
            ImGui.TextColored(color, text);
        }

        private void PrintTextColumn(int column, string text)
        {
            PrintTextColumn(column, text, this._defaultColor);
        }

        private Vector4 GetLogColor(int log)
        {
            return log switch
            {
                < 0 => this._logColors["Default"],
                < 25 => this._logColors["Grey"],
                < 50 => this._logColors["Green"],
                < 75 => this._logColors["Blue"],
                < 95 => this._logColors["Magenta"],
                < 99 => this._logColors["Orange"],
                99 => this._logColors["Pink"],
                100 => this._logColors["Yellow"],
                _ => this._logColors["Default"],
            };
        }

        internal void SetCharacterAndFetchLogs(CharacterData character)
        {
            this._selectedCharacterData = character;
            this._errorMessage = "";
            this._hasLoadingFailed = false;
            this._plugin.FetchLogs(this._selectedCharacterData);
        }

        internal void SetErrorMessage(string errorMessage)
        {
            this._errorMessage = errorMessage;
        }
    }
}
