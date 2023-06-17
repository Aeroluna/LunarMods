namespace LunarMods.Models;

public enum Visibility
{
    Public = 0,
    Deleted = 1,
    Unlisted = 2,
    Private = 3
}

public class Mod
{
    public string Id { get; init; } = string.Empty;

    public int Visibility { get; set; }

    public string Name { get; init; } = "Placeholder";

    public ulong Author { get; init; }

    public string Overview { get; set; } = "It does stuff.";

    public string Category { get; set; } = "Other";

    public string PreviewImage { get; set; } = string.Empty;

    public string Description { get; set; } = "It does really cool stuff.";

    public string Repository { get; set; } = string.Empty;

    public string CreatedDate { get; init; } = string.Empty;

    public string LastUpdateDate { get; set; } = string.Empty;

    public string? LatestVersion { get; set; }
}
