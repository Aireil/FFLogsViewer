using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using FFLogsViewer.Model;
using Newtonsoft.Json;

namespace FFLogsViewer;

[Serializable]
public class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    public const int CurrentConfigVersion = 1;
    public int Version { get; set; } = CurrentConfigVersion;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool ContextMenu { get; set; } = true;
    public bool ContextMenuStreamer { get; set; }
    public bool ContextMenuPartyView { get; set; } = true;
    public bool ContextMenuAlwaysPartyView { get; set; }
    public bool OpenInBrowser { get; set; }
    public bool ShowTomestoneOption { get; set; } = true;
    public string ContextMenuButtonName { get; set; } = "Search FF Logs";
    public bool IsDefaultViewParty { get; set; }
    public bool HideInCombat { get; set; }
    public bool IsDefaultLayout { get; set; } = true;
    public bool IsHistoricalDefault { get; set; } = true;
    public bool IsEncounterLayout { get; set; } = true;
    public bool IsCachingEnabled { get; set; } = true;
    public bool IsAllJobsDefault { get; set; } = true;
    public int NbOfDecimalDigits { get; set; }
    public StatType? DefaultStatTypePartyView { get; set; }
    public LayoutEntry? DefaultEncounterPartyView { get; set; }
    public List<LayoutEntry> Layout { get; set; } = [];
    public List<Stat> Stats { get; set; } = [];
    public Metric Metric { get; set; } = new() { Name = "rDPS", InternalName = "rdps" };
    public Style Style { get; set; } = new();
    public OpenWith OpenWith { get; set; } = new();
    public bool IsUpdateDismissed2213 { get; set; }

    public void Save()
    {
        Service.Interface.SavePluginConfig(this);
    }

    public void Initialize()
    {
        if (this.IsDefaultLayout || this.Layout.Count == 0)
        {
            this.SetDefaultLayout();
        }

        if (this.Stats.Count == 0)
        {
            this.Stats.AddRange(GetDefaultStats());
        }

        this.Upgrade();
    }

    public void Upgrade()
    {
        // all stars stats
        if (this.Version == 0)
        {
            var defaultStats = GetDefaultStats();
            if (this.Stats.Count < defaultStats.Count)
            {
                for (var i = this.Stats.Count; i < defaultStats.Count; i++)
                {
                    this.Stats.Add(defaultStats[i]);
                }
            }

            this.Version++;
            this.Save();
        }
    }

    public void SetDefaultLayout()
    {
        this.Layout = GetDefaultLayout();
        this.IsDefaultLayout = true;
    }

    private static List<LayoutEntry> GetDefaultLayout()
    {
        return
        [
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "ACC L-H", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-" },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Black Cat", EncounterId = 93, Difficulty = "Normal", DifficultyId = 101 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Honey B. Lovely", EncounterId = 94, Difficulty = "Normal", DifficultyId = 101 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Brute Bomber", EncounterId = 95, Difficulty = "Normal", DifficultyId = 101 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Wicked Thunder", EncounterId = 96, Difficulty = "Normal", DifficultyId = 101 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "Ultimates (DT)", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Ultimates (Legacy)", ZoneId = 59, Encounter = "The Unending Coil of Bahamut", EncounterId = 1073, Difficulty = "Normal", DifficultyId = 100, Alias = "UCoB", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Ultimates (Legacy)", ZoneId = 59, Encounter = "The Weapon's Refrain", EncounterId = 1074, Difficulty = "Normal", DifficultyId = 100, Alias = "UwU", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Ultimates (Legacy)", ZoneId = 59, Encounter = "The Epic of Alexander", EncounterId = 1075, Difficulty = "Normal", DifficultyId = 100, Alias = "TEA", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Ultimates (Legacy)", ZoneId = 59, Encounter = "Dragonsong's Reprise", EncounterId = 1076, Difficulty = "Normal", DifficultyId = 100, Alias = "DSR", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Ultimates (Legacy)", ZoneId = 59, Encounter = "The Omega Protocol", EncounterId = 1077, Difficulty = "Normal", DifficultyId = 100, Alias = "TOP", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Futures Rewritten", ZoneId = 65, Encounter = "Futures Rewritten", EncounterId = 1079, Difficulty = "Normal", DifficultyId = 100, Alias = "FRU", SwapId = "DT ult", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "Ultimates (EW)", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-", SwapId = "DT ult", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Endwalker", Zone = "Ultimates (Legacy)", ZoneId = 43, Encounter = "The Unending Coil of Bahamut", EncounterId = 1060, Difficulty = "Normal", DifficultyId = 100, Alias = "UCoB", SwapId = "DT ult", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Endwalker", Zone = "Ultimates (Legacy)", ZoneId = 43, Encounter = "The Weapon's Refrain", EncounterId = 1061, Difficulty = "Normal", DifficultyId = 100, Alias = "UwU", SwapId = "DT ult", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Endwalker", Zone = "Ultimates (Legacy)", ZoneId = 43, Encounter = "The Epic of Alexander", EncounterId = 1062, Difficulty = "Normal", DifficultyId = 100, Alias = "TEA", SwapId = "DT ult", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Endwalker", Zone = "Dragonsong's Reprise", ZoneId = 45, Encounter = "Dragonsong's Reprise", EncounterId = 1065, Difficulty = "Normal", DifficultyId = 100, Alias = "DSR", SwapId = "DT ult", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Endwalker", Zone = "The Omega Protocol", ZoneId = 53, Encounter = "The Omega Protocol", EncounterId = 1068, Difficulty = "Normal", DifficultyId = 100, Alias = "TOP", SwapId = "DT ult", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "Extremes", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-" },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials (Extreme)", ZoneId = 58, Encounter = "Valigarmanda", EncounterId = 1071, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials (Extreme)", ZoneId = 58, Encounter = "Zoraal Ja", EncounterId = 1072, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials (Extreme)", ZoneId = 58, Encounter = "Queen Eternal", EncounterId = 1078, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "Chaotic", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-" },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Alliance Raids (Chaotic)", ZoneId = 66, Encounter = "Cloud of Darkness", EncounterId = 2061, Difficulty = "Normal", DifficultyId = 100 },
        ];
    }

    private static List<Stat> GetDefaultStats()
    {
        return
        [
            new Stat { Name = "Best", Type = StatType.Best, IsEnabled = true },
            new Stat { Alias = "Med.", Name = "Median", Type = StatType.Median, IsEnabled = true },
            new Stat { Name = "Kills", Type = StatType.Kills, IsEnabled = true },
            new Stat { Name = "Fastest", Type = StatType.Fastest, IsEnabled = false },
            new Stat { Alias = "/metric/", Name = "Best Metric", Type = StatType.BestAmount, IsEnabled = false },
            new Stat { Name = "Job", Type = StatType.Job, IsEnabled = true },
            new Stat { Name = "Best Job", Type = StatType.BestJob, IsEnabled = false },
            new Stat { Alias = "ASP", Name = "All Stars Points", Type = StatType.AllStarsPoints, IsEnabled = false },
            new Stat { Alias = "ASP R", Name = "All Stars Rank", Type = StatType.AllStarsRank, IsEnabled = false },
            new Stat { Alias = "ASP R%", Name = "All Stars Rank %", Type = StatType.AllStarsRankPercent, IsEnabled = false },
        ];
    }
}
