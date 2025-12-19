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
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "ACC H", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-", SwapId = "7.4", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "AAC Heavyweight", ZoneId = 73, Encounter = "Vamp Fatale", EncounterId = 101, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Heavyweight", ZoneId = 73, Encounter = "Red Hot and Deep Blue", EncounterId = 102, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Heavyweight", ZoneId = 73, Encounter = "The Tyrant", EncounterId = 103, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Heavyweight", ZoneId = 73, Encounter = "The Lindwurm", EncounterId = 104, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 0 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "ACC C", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-", SwapId = "7.4", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Cruiserweight", ZoneId = 68, Encounter = "Dancing Green", EncounterId = 97, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Cruiserweight", ZoneId = 68, Encounter = "Sugar Riot", EncounterId = 98, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Cruiserweight", ZoneId = 68, Encounter = "Brute Abombinator", EncounterId = 99, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Cruiserweight", ZoneId = 68, Encounter = "Howling Blade", EncounterId = 100, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 1 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "ACC L-H", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-", SwapId = "7.4", SwapNumber = 2 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Black Cat", EncounterId = 93, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 2 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Honey B. Lovely", EncounterId = 94, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 2 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Brute Bomber", EncounterId = 95, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 2 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "ACC Light-heavyweight", ZoneId = 62, Encounter = "Wicked Thunder", EncounterId = 96, Difficulty = "Savage", DifficultyId = 101, SwapId = "7.4", SwapNumber = 2 },
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
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials II (Extreme)", ZoneId = 67, Encounter = "Zelenia", EncounterId = 1080, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials II (Extreme)", ZoneId = 67, Encounter = "Necron", EncounterId = 1081, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials II (Extreme)", ZoneId = 67, Encounter = "Arkveld", EncounterId = 1082, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials III (Extreme)", ZoneId = 72, Encounter = "Doomtrain", EncounterId = 1083, Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Header, Alias = "Others", Expansion = "-", Zone = "-", Encounter = "-", Difficulty = "-" },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Alliance Raids (Chaotic)", ZoneId = 66, Encounter = "Cloud of Darkness", EncounterId = 2061, Alias = "CoD (Chaotic)", Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Trials (Unreal)", ZoneId = 64, Encounter = "Tsukuyomi", EncounterId = 3012, Alias = "Tsukuyomi (Unreal)", Difficulty = "Normal", DifficultyId = 100 },
            new LayoutEntry { Type = LayoutEntryType.Encounter, Expansion = "Dawntrail", Zone = "Deep Dungeons", ZoneId = 71, Encounter = "Eminent Grief", EncounterId = 4548, Alias = "Quantum (40)", Difficulty = "Savage", DifficultyId = 101 },
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
