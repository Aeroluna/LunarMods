namespace LunarMods.Models;

public class AllFileVersionDetails
{
    public ModDetails Mod { get; init; } = null!;

    public IEnumerable<FileVersionDetails> Versions { get; init; } = Enumerable.Empty<FileVersionDetails>();
}

public class FileVersionDetails
{
    public string Id { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public int Alpha { get; init; }

    public List<Dependency> Dependencies { get; init; } = new();

    public List<Dependency> Conflicts { get; init; } = new();

    public int Status { get; init; }

    public string UploadDate { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileSize { get; init; } = string.Empty;

    public string Changelog { get; init; } = string.Empty;

    public string GameVersions { get; init; } = string.Empty;

    // ReSharper disable once InconsistentNaming
    public string MD5 { get; init; } = string.Empty;

    // ReSharper disable once InconsistentNaming
    public string SHA256 { get; init; } = string.Empty;

    public string FileTree { get; init; } = string.Empty;

    public class Dependency
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }
}
