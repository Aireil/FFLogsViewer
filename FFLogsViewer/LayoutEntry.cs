using System;
using FFLogsViewer.Model;

namespace FFLogsViewer;

public class LayoutEntry : ICloneable
{
    public LayoutEntryType Type { get; set; } = LayoutEntryType.None;
    public string Alias { get; set; } = string.Empty;
    public string Expansion { get; set; } = string.Empty;
    public string Zone { get; set;  } = string.Empty;
    public int ZoneId { get; set; }
    public string Encounter { get; set; } = string.Empty;
    public int EncounterId { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public int DifficultyId { get; set; }
    public string SwapId { get; set; } = string.Empty;
    public int SwapNumber { get; set; }
    public bool IsForcingADPS { get; set; }

    public LayoutEntry()
    {
    }

    protected LayoutEntry(LayoutEntry layoutEntry)
    {
        this.Type = layoutEntry.Type;
        this.Alias = layoutEntry.Alias;
        this.Expansion = layoutEntry.Expansion;
        this.Zone = layoutEntry.Zone;
        this.ZoneId = layoutEntry.ZoneId;
        this.Encounter = layoutEntry.Encounter;
        this.EncounterId = layoutEntry.EncounterId;
        this.Difficulty = layoutEntry.Difficulty;
        this.DifficultyId = layoutEntry.DifficultyId;
        this.SwapId = layoutEntry.SwapId;
        this.SwapNumber = layoutEntry.SwapNumber;
        this.IsForcingADPS = layoutEntry.IsForcingADPS;
    }

    public static LayoutEntry CreateEncounter(LayoutEntry? layoutEntry = null)
    {
        return new LayoutEntry
        {
            Type = LayoutEntryType.Encounter,
            Alias = layoutEntry?.Alias ?? string.Empty,
            Expansion = layoutEntry?.Expansion ?? "-",
            Zone = layoutEntry?.Zone ?? "-",
            ZoneId = layoutEntry?.ZoneId ?? 0,
            Encounter = layoutEntry?.Encounter ?? "-",
            EncounterId = layoutEntry?.EncounterId ?? 0,
            Difficulty = layoutEntry?.Difficulty ?? "-",
            DifficultyId = layoutEntry?.DifficultyId ?? 0,
            SwapId = layoutEntry?.SwapId ?? string.Empty,
            SwapNumber = layoutEntry?.SwapNumber ?? 0,
            IsForcingADPS = layoutEntry?.IsForcingADPS ?? false,
        };
    }

    public static LayoutEntry CreateHeader(LayoutEntry? layoutEntry = null)
    {
        return new LayoutEntry
        {
            Type = LayoutEntryType.Header,
            Alias = layoutEntry?.Alias ?? string.Empty,
            Expansion = "-",
            Zone = "-",
            Encounter = "-",
            Difficulty = "-",
            SwapId = layoutEntry?.SwapId ?? string.Empty,
            SwapNumber = layoutEntry?.SwapNumber ?? 0,
        };
    }

    public LayoutEntry CloneEncounter()
    {
        return CreateEncounter(this);
    }

    public LayoutEntry CloneHeader()
    {
        return CreateHeader(this);
    }

    public object Clone()
    {
        return new LayoutEntry(this);
    }

    public bool IsEncounterValid()
    {
        return this.Expansion != "-" &&
               this.Zone != "-" &&
               this.Encounter != "-" &&
               this.Difficulty != "-";
    }

    public bool Compare(LayoutEntry layoutEntry)
    {
        return this.Type == layoutEntry.Type &&
               this.Alias == layoutEntry.Alias &&
               this.Expansion == layoutEntry.Expansion &&
               this.Zone == layoutEntry.Zone &&
               this.ZoneId == layoutEntry.ZoneId &&
               this.Encounter == layoutEntry.Encounter &&
               this.EncounterId == layoutEntry.EncounterId &&
               this.Difficulty == layoutEntry.Difficulty &&
               this.DifficultyId == layoutEntry.DifficultyId &&
               this.SwapId == layoutEntry.SwapId &&
               this.SwapNumber == layoutEntry.SwapNumber &&
               this.IsForcingADPS == layoutEntry.IsForcingADPS;
    }
}
