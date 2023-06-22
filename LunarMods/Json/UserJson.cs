using System.Text.Json.Serialization;

namespace LunarMods.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
public class UserJson
{
    public UserJson(ulong id, string username)
    {
        Id = id;
        Username = username;
    }

    [JsonPropertyName("id")]
    public ulong Id { get; }

    [JsonPropertyName("username")]
    public string Username { get; }
}
