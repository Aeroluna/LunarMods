using LunarMods.Data;
using LunarMods.Models;
using LunarMods.Utilities;

namespace LunarMods.Services;

public class GameVersionService
{
    private readonly ApplicationDbContext _context;

    private List<string> _validVersions = new();

    public GameVersionService(ApplicationDbContext context)
    {
        _context = context;
        Refresh();
    }

    public string LatestGameVersion { get; private set; } = string.Empty;

    public void Refresh()
    {
        List<GameVersion> gameVersions = _context.GameVersions.ToList();
        _validVersions = gameVersions.Select(n => n.Version).ToList();
        List<GameVersion> releaseVersions = gameVersions.Where(n => (Alpha)n.Alpha == Alpha.Release).ToList();
        LatestGameVersion = releaseVersions.MaxBy(n => Version.Parse(n.Version))?.Version ?? string.Empty;
    }

    public bool ValidateGameVersions(FileVersionInput input)
    {
        return input.GameVersions.SSplit().All(n => _validVersions.Contains(n));
    }

    public bool ContainsLatestVersion(string str)
    {
        return str.SSplit().Contains(LatestGameVersion);
    }
}
