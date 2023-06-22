using System.Text.Json.Serialization;

namespace LunarMods.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
public class ModJson
{
    public ModJson(
        string id,
        string name,
        UserJson author,
        string overview,
        string category,
        string repository,
        string createdDate,
        string lastUpdatedDate,
        string latestVersion,
        List<VersionJson> versions)
    {
        Id = id;
        Name = name;
        Author = author;
        Overview = overview;
        Category = category;
        Repository = repository;
        CreatedDate = createdDate;
        LastUpdatedDate = lastUpdatedDate;
        LatestVersion = latestVersion;
        Versions = versions;
    }

    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("author")]
    public UserJson Author { get; }

    [JsonPropertyName("overview")]
    public string Overview { get; }

    [JsonPropertyName("category")]
    public string Category { get; }

    [JsonPropertyName("repository")]
    public string Repository { get; }

    [JsonPropertyName("createdDate")]
    public string CreatedDate { get; }

    [JsonPropertyName("updatedDate")]
    public string LastUpdatedDate { get; }

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; }

    [JsonPropertyName("versions")]
    public List<VersionJson> Versions { get; }
}
