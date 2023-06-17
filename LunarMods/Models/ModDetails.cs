namespace LunarMods.Models;

public class ModDetails
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string AuthorName { get; init; } = string.Empty;

    public string Overview { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string PreviewImage { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Repository { get; init; } = string.Empty;

    public string CreatedDate { get; init; } = string.Empty;

    public string LastUpdateDate { get; init; } = string.Empty;

    public FileVersionDetails? LatestVersion { get; init; }

    public int Visibility { get; init; }

    public bool CanUpload { get; init; }

    public bool CanModerate { get; init; }
}
