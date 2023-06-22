using System.ComponentModel.DataAnnotations;

namespace LunarMods.Models;

public class GameVersion
{
    [Key]
    public string Version { get; init; } = string.Empty;

    public int Alpha { get; set; }
}
