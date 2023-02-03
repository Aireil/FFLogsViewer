namespace FFLogsViewer.Model;

public class Metric
{
    public string Name = null!;
    public string InternalName = null!;
    public string Abbreviation
    {
        get => this.abbreviation ?? this.Name;
        init => this.abbreviation = value;
    }

    private readonly string? abbreviation;
}
