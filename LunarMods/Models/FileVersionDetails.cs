namespace LunarMods.Models;

public class AllFileVersionDetails
{
    public ModDetails Mod { get; init; }

    public IEnumerable<FileVersionDetails> Versions { get; init; }
}

public class FileVersionDetails
{
    public string Id { get; init; }

    public int Alpha { get; init; }

    public List<Dependency> Dependencies { get; init; }

    public List<Dependency> Conflicts { get; init; }

    public string Version { get; init; }

    public int Status { get; init; }

    public string UploadDate { get; init; }

    public string FileDownloadName { get; init; }

    public string File { get; init; }

    public string FileSize { get; init; }

    public string Changelog { get; init; }

    public string GameVersions { get; init; }

    public string SHA256 { get; init; }

    public class Dependency
    {
        public string Id { get; init; }
        public string Name { get; init; }
    }
}
