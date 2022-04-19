namespace FFLogsViewer.Model;

public class Encounter
{
    public int Id;
    public int Difficulty;
    public int? Best;
    public int? Median;
    public int? Kills;
    public int? Fastest;
    public int? BestAmount;
    public bool IsLockedIn;
    public string? Metric;
    public Job? Job;
    public Job? BestJob;
}
