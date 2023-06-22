using System.Security.Claims;
using LunarMods.Models;
using LunarMods.Utilities;
using Microsoft.EntityFrameworkCore;

namespace LunarMods.Data;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; init; } = null!;

    public DbSet<Mod> Mods { get; init; } = null!;

    public DbSet<FileVersion> FileVersions { get; init; } = null!;

    public DbSet<GameVersion> GameVersions { get; init; } = null!;

    public async Task<Mod?> GetMod(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 8)
        {
            return null;
        }

        Mod? mod = await Mods.FindAsync(id);
        if (mod == null || (Visibility)mod.Visibility == Visibility.Deleted)
        {
            return null;
        }

        return mod;
    }

    public async Task<FileVersion?> GetVersion(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length != 8)
        {
            return null;
        }

        FileVersion? ver = await FileVersions.FindAsync(id);
        if (ver == null || (Visibility)ver.Visibility == Visibility.Deleted)
        {
            return null;
        }

        return ver;
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public async Task<ModDetails> Convert(ClaimsPrincipal user, Mod mod)
    {
        string authorName = (await Users.FindAsync(mod.Author))?.Username ?? "N/A";
        FileVersion? latest = await FileVersions.FindAsync(mod.LatestVersion);
        return new ModDetails
        {
            Id = mod.Id,
            Name = mod.Name,
            AuthorName = authorName,
            Overview = mod.Overview,
            Category = mod.Category,
            PreviewImage = mod.PreviewImage,
            Description = mod.Description,
            Repository = mod.Repository,
            CreatedDate = mod.CreatedDate.FormatDate(),
            LastUpdateDate = mod.LastUpdateDate.FormatDate(),
            LatestVersion = latest == null ? null : Convert(latest),
            Visibility = mod.Visibility,
            CanUpload = user.CanUpload(mod),
            CanModerate = user.CanModerate(mod)
        };
    }

    public FileVersionDetails Convert(FileVersion ver)
    {
        List<FileVersionDetails.Dependency> HandleDependencies(string dependencies)
        {
            return (from dependencyId in dependencies.SSplit()
                let dependency = Mods.Find(dependencyId)
                where dependency != null
                select new FileVersionDetails.Dependency {Id = dependencyId, Name = dependency.Name}).ToList();
        }

        return new FileVersionDetails
        {
            Id = ver.Id,
            Alpha = ver.Alpha,
            Changelog = ver.Changelog,
            Dependencies = HandleDependencies(ver.Dependencies),
            Conflicts = HandleDependencies(ver.Conflicts),
            FileName = ver.FileName,
            FileSize = ver.FileSize.BytesToString(),
            GameVersions = ver.GameVersions,
            MD5 = ver.MD5,
            SHA256 = ver.SHA256,
            FileTree = ZipUtil.Format(ver.Files.Split('\n')),
            Status = ver.Status,
            UploadDate = ver.UploadDate.FormatDate(),
            Version = ver.Version
        };
    }

    public bool CompareLatestVersion(FileVersion fileVersion, Mod mod)
    {
        if ((Alpha)fileVersion.Alpha != Alpha.Release)
        {
            return false;
        }

        if (mod.LatestVersion == null)
        {
            return true;
        }

        FileVersion? currentLatest = FileVersions.Find(mod.LatestVersion);
        if (currentLatest == null)
        {
            return true;
        }

        Version currentVersion = Version.Parse(currentLatest.Version);
        Version newVersion = Version.Parse(fileVersion.Version);
        if (newVersion.CompareTo(currentVersion) <= 0)
        {
            return false;
        }

        if (fileVersion.GameVersions.SSplit().Max(Version.Parse) >
            currentLatest.GameVersions.SSplit().Max(Version.Parse))
        {
            return true;
        }

        return fileVersion.Status >= currentLatest.Status;
    }

    public IEnumerable<FileVersion> GetModVersions(Mod mod)
    {
        return FileVersions.Where(n => n.Mod == mod.Id && (Alpha)n.Alpha == Alpha.Release && (Visibility)n.Visibility != Visibility.Deleted);
    }

    public static FileVersion? GetLatestVersion(IEnumerable<FileVersion> fileVersions)
    {
        FileVersion? maxVersion = null;
        Version? versionMax = null;
        Version? gameVersionMax = null;
        int statusMax = -1;
        foreach (FileVersion version in fileVersions)
        {
            Version newGameVer = version.GameVersions.SSplit().Max(Version.Parse) ?? throw new InvalidOperationException("Unparseable game version.");
            Version newVer = Version.Parse(version.Version);

            void SetNewMax()
            {
                maxVersion = version;
                versionMax = newVer;
                gameVersionMax = newGameVer;
                statusMax = version.Status;
            }

            if (newVer.CompareTo(versionMax) <= 0)
            {
                continue;
            }

            if (newGameVer.CompareTo(gameVersionMax) > 0)
            {
                SetNewMax();
                continue;
            }

            if (version.Status < statusMax)
            {
                continue;
            }

            SetNewMax();
        }

        return maxVersion;
    }
}
