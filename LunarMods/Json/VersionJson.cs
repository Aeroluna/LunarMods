using System.Text.Json.Serialization;

namespace LunarMods.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
public class VersionJson
{
    public VersionJson(
        string version,
        int alpha,
        List<string> dependencies,
        List<string> conflicts,
        int status,
        string uploadDate,
        string url,
        long fileSize,
        string changelog,
        List<string> gameVersions,
        string md5,
        string sha256,
        string[] files)
    {
        Version = version;
        Alpha = alpha;
        Dependencies = dependencies;
        Conflicts = conflicts;
        Status = status;
        UploadDate = uploadDate;
        Url = url;
        FileSize = fileSize;
        Changelog = changelog;
        GameVersions = gameVersions;
        Md5 = md5;
        Sha256 = sha256;
        Files = files;
    }

    [JsonPropertyName("version")]
    public string Version { get; }

    [JsonPropertyName("alpha")]
    public int Alpha { get; }

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; }

    [JsonPropertyName("conflicts")]
    public List<string> Conflicts { get; }

    [JsonPropertyName("status")]
    public int Status { get; }

    [JsonPropertyName("uploadDate")]
    public string UploadDate { get; }

    [JsonPropertyName("url")]
    public string Url { get; }

    [JsonPropertyName("fileSize")]
    public long FileSize { get; }

    [JsonPropertyName("changelog")]
    public string Changelog { get; }

    [JsonPropertyName("gameVersions")]
    public List<string> GameVersions { get; }

    [JsonPropertyName("md5")]
    public string Md5 { get; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; }

    [JsonPropertyName("files")]
    public string[] Files { get; }
}
