namespace LunarMods.Models;

public enum Status
{
    Unapproved,
    Trusted,
    Verified
}

public enum Alpha
{
    Release,
    PreRelease,
}

public class FileVersion
{
    public string Id { get; init; }

    public string Mod { get; init; }

    public int Visibility { get; set; }

    public int Alpha { get; set; }

    public string Dependencies { get; set; }

    public string Conflicts { get; set; }

    public string Version { get; set; }

    public int Status { get; set; }

    public string UploadDate { get; init; }

    public string File { get; init; }

    public long FileSize { get; init; }

    public string Changelog { get; set; }

    public string GameVersions { get; set; }

    public string SHA256 { get; init; }
}
