using System.Text.Json.Serialization;

namespace LunarMods.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
public class GameVersionJson
{
    public GameVersionJson(string version, int alpha)
    {
        Version = version;
        Alpha = alpha;
    }

    [JsonPropertyName("version")]
    public string Version { get; }

    [JsonPropertyName("alpha")]
    public int Alpha { get; }
}
