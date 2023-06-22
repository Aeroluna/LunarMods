namespace LunarMods.Models;

public enum Status
{
    Unapproved,
    Trusted,
    Approved
}

public enum Alpha
{
    Release,
    PreRelease
}

public class FileVersion
{
    public string Id { get; init; } = string.Empty;

    public string Mod { get; init; } = string.Empty;

    public int Visibility { get; set; }

    public string Version { get; init; } = string.Empty;

    public int Alpha { get; set; }

    public string Dependencies { get; set; } = string.Empty;

    public string Conflicts { get; set; } = string.Empty;

    public int Status { get; set; }

    public string UploadDate { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string Changelog { get; set; } = string.Empty;

    public string GameVersions { get; set; } = string.Empty;

    // ReSharper disable once InconsistentNaming
    public string MD5 { get; init; } = string.Empty;

    // ReSharper disable once InconsistentNaming
    public string SHA256 { get; init; } = string.Empty;

    public string Files { get; init; } = string.Empty;
}
