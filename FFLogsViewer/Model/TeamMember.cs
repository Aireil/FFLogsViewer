namespace FFLogsViewer.Model;

public class TeamMember
{
    public string FirstName = null!;
    public string LastName = null!;
    public string World = null!;
    public uint JobId;

    /// <summary>
    /// Null if in the local player party.
    /// </summary>
    public uint? AllianceIndex;
}
