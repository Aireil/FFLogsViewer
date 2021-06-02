using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Plugin;
using ImGuiNET;

namespace FFLogsViewer
{
    internal class PluginUi : IDisposable
    {
        private const float WindowHeight = 458;
        private const float ReducedWindowHeight = 88;
        private const float WindowWidth = 407;
        private readonly string[] _characterInput = new string[3];
        private readonly Vector4 _defaultColor = new(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Dictionary<string, Vector4> _jobColors = new();
        private readonly Dictionary<string, Vector4> _logColors = new();
        private readonly Plugin _plugin;

        private float _bossesColumnWidth;

        private string _errorMessage = "";

        private bool _hasLoadingFailed;

        private bool _isLinkClicked;
        private float _jobsColumnWidth;
        private float _logsColumnWidth;
        private CharacterData _selectedCharacterData = new();
        private bool _settingsVisible;

        private bool _visible;

        internal PluginUi(Plugin plugin)
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

            ImGui.SetNextWindowSize(new Vector2(400, 120), ImGuiCond.Always);
            if (ImGui.Begin("FF Logs Viewer Config", ref this._settingsVisible,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse))
            {
                var contextMenu = this._plugin.Configuration.ContextMenu;
                if (ImGui.Checkbox("Search when opening context menus", ref contextMenu))
                {
                    this._plugin.ToggleContextMenuButton(contextMenu);
                    this._plugin.Configuration.ContextMenu = contextMenu;
                    this._plugin.Configuration.Save();
                }
            }

            ImGui.End();
        }

        private void DrawMainWindow()
        {
            if (!this.Visible) return;

            ImGui.SetNextWindowSize(new Vector2(WindowWidth, ReducedWindowHeight), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("FF Logs Viewer", ref this._visible,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Columns(4, "InputColumns", true);

                var buttonsWidth = (ImGui.CalcTextSize("Target") + ImGui.CalcTextSize("Clipboard")).X + 40.0f;
                var colWidth = (ImGui.GetWindowWidth() - buttonsWidth) / 3.0f;
                var sizeMin = Math.Max(ImGui.CalcTextSize(this._selectedCharacterData.FirstName).X,
                    Math.Max(ImGui.CalcTextSize(this._selectedCharacterData.LastName).X,
                        ImGui.CalcTextSize(this._selectedCharacterData.WorldName).X));
                var idealWindowWidth = sizeMin * 3 + buttonsWidth + 73.0f;
                if (idealWindowWidth < WindowWidth) idealWindowWidth = WindowWidth;
                float idealWindowHeight;
                if (this._selectedCharacterData.IsEveryLogsReady && !this._hasLoadingFailed)
                    idealWindowHeight = WindowHeight;
                else
                    idealWindowHeight = ReducedWindowHeight;
                ImGui.SetWindowSize(new Vector2(idealWindowWidth, idealWindowHeight));

                ImGui.SetColumnWidth(0, colWidth);
                ImGui.SetColumnWidth(1, colWidth);
                ImGui.SetColumnWidth(2, colWidth);
                ImGui.SetColumnWidth(3, buttonsWidth);

                ImGui.PushItemWidth(colWidth - 15);
                this._characterInput[0] = this._selectedCharacterData.FirstName;
                ImGui.InputTextWithHint("##FirstName", "First Name", ref this._characterInput[0], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                this._selectedCharacterData.FirstName = this._characterInput[0];
                ImGui.PopItemWidth();

                ImGui.NextColumn();
                ImGui.PushItemWidth(colWidth - 15);
                this._characterInput[1] = this._selectedCharacterData.LastName;
                ImGui.InputTextWithHint("##LastName", "Last Name", ref this._characterInput[1], 256,
                    ImGuiInputTextFlags.CharsNoBlank);
                this._selectedCharacterData.LastName = this._characterInput[1];
                ImGui.PopItemWidth();

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
                if (this._errorMessage == "")
                {
                    if (this._selectedCharacterData.IsEveryLogsReady)
                    {
                        var nameVector =
                            ImGui.CalcTextSize(
                                $"Viewing logs of {this._selectedCharacterData.LoadedFirstName} {this._selectedCharacterData.LoadedLastName}@{this._selectedCharacterData.LoadedWorldName}.");
                        ImGui.SetCursorPosX(ImGui.GetWindowWidth() / 2 - nameVector.X / 2);
                        nameVector.X -= 7; // A bit too large on right side
                        nameVector.Y += 1;
                        ImGui.Selectable(
                            $"Viewing {this._selectedCharacterData.LoadedFirstName} {this._selectedCharacterData.LoadedLastName}@{this._selectedCharacterData.LoadedWorldName}'s logs.",
                            ref this._isLinkClicked, ImGuiSelectableFlags.None, nameVector);

                        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to open on FF Logs.");

                        if (this._isLinkClicked)
                        {
                            Process.Start(
                                $"https://www.fflogs.com/character/{this._selectedCharacterData.RegionName}/{this._selectedCharacterData.WorldName}/{this._selectedCharacterData.FirstName} {this._selectedCharacterData.LastName}");
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

                ImGui.SameLine();

                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize("Search").X * 1.5f + 4.0f);

                if (ImGui.Button("Search"))
                {
                    if (this._selectedCharacterData.IsCharacterReady())
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

                    PrintTextColumn(1, "Eden's Promise");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Cloud of Darkness");
                    PrintTextColumn(1, "Shadowkeeper");
                    PrintTextColumn(1, "Fatebreaker");
                    PrintTextColumn(1, "Eden's Promise");
                    PrintTextColumn(1, "Oracle of Darkness");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Ultimates");
                    ImGui.Spacing();
                    PrintTextColumn(1, "TEA");
                    PrintTextColumn(1, "UwU");
                    PrintTextColumn(1, "UCoB");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Trials (Extreme)");
                    ImGui.Spacing();
                    PrintTextColumn(1, "The Emerald I");
                    PrintTextColumn(1, "The Emerald II");
                    PrintTextColumn(1, "The Diamond");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Unreal");
                    ImGui.Spacing();
                    PrintTextColumn(1, "Leviathan");

                    try
                    {
                        ImGui.NextColumn();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Best, 2);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Best, 2);
                        ImGui.Spacing();
                        PrintTextColumn(2, "Best");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Best, 2);

                        ImGui.NextColumn();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Median, 3);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Median, 3);
                        ImGui.Spacing();
                        PrintTextColumn(3, "Med.");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Median, 3);

                        ImGui.NextColumn();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Kills, 4);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Kills, 4);
                        ImGui.Spacing();
                        PrintTextColumn(4, "Kills");
                        ImGui.Spacing();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Kills, 4);

                        ImGui.NextColumn();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.CloudOfDarkness, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.Shadowkeeper, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.Fatebreaker, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.EdensPromise, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.OracleOfDarkness, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.Tea, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.UwU, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.UCoB, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponI, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.TheEmeraldWeaponIi, CharacterData.DataType.Job, 5);
                        PrintDataColumn(CharacterData.BossesId.TheDiamondWeapon, CharacterData.DataType.Job, 5);
                        ImGui.Separator();
                        PrintTextColumn(5, "Job");
                        ImGui.Separator();
                        PrintDataColumn(CharacterData.BossesId.LeviathanUnreal, CharacterData.DataType.Job, 5);

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
            string text;
            var color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            int log;
            switch (dataType)
            {
                case CharacterData.DataType.Best:
                    if (!this._selectedCharacterData.Bests.TryGetValue((int) bossId, out log))
                        throw new ArgumentNullException($"Best log not found for boss ({bossId}).");
                    text = log == 0 ? "-" : log.ToString();
                    color = GetLogColor(log);
                    break;

                case CharacterData.DataType.Median:
                    if (!this._selectedCharacterData.Medians.TryGetValue((int) bossId, out log))
                        throw new ArgumentNullException($"Median log not found for boss ({bossId}).");
                    text = log == 0 ? "-" : log.ToString();
                    color = GetLogColor(log);
                    break;
                case CharacterData.DataType.Kills:
                    if (!this._selectedCharacterData.Kills.TryGetValue((int) bossId, out log))
                        throw new ArgumentNullException($"Median log not found for boss ({bossId}).");
                    text = log == 0 ? "-" : log.ToString();
                    break;
                case CharacterData.DataType.Job:
                    if (!this._selectedCharacterData.Jobs.TryGetValue((int) bossId, out var job))
                        throw new ArgumentNullException($"Job not found for boss ({bossId}).");
                    if (!this._jobColors.TryGetValue(job, out color)) color = this._jobColors["Default"];
                    text = job;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }


            PrintTextColumn(column, text, color);
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
                0 => this._logColors["Default"],
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