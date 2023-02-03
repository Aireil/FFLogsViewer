namespace FFLogsViewer.Model;

public class Partition
{
    public string Name { get; init; } = null!;
    public int Id { get; init; }
    public string Abbreviation
    {
        get => this.abbreviation ?? this.Name;
        init => this.abbreviation = value;
    }

    private readonly string? abbreviation;
}
