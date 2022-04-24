namespace FFLogsViewer.Model;

public class Encounter
{
    public int Id;
    public int Difficulty;
    public float? Best;
    public float? Median;
    public int? Kills;
    public int? Fastest;
    public int? BestAmount;
    public bool IsLockedIn = true;
    public string? Metric;
    public Job? Job;
    public Job? BestJob;
    public float? AllStarsPoints;
    public int? AllStarsRank;
    public float? AllStarsRankPercent;
}
