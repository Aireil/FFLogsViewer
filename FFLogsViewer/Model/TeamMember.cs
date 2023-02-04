namespace FFLogsViewer.Model;

public class TeamMember
{
    private readonly string name = null!;
    public string Name
    {
        get => this.name;
        init
        {
            this.name = value;
            var splitName = this.name.Split(' ');
            this.Abbreviation = splitName.Length >= 2 ? $"{splitName[0][0]}. {splitName[1][0]}." : this.name[0].ToString();
        }
    }

    public string Abbreviation = null!;
    public string World = null!;
    public uint JobId;
    public bool IsInParty;
}
