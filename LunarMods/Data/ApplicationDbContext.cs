using System.Security.Claims;
using LunarMods.Models;
using LunarMods.Utilities;
using Microsoft.EntityFrameworkCore;

namespace LunarMods.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; init; }

    public DbSet<Mod> Mods { get; init; }

    public DbSet<FileVersion> FileVersions { get; init; }

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
            LatestVersion = latest == null ? null : Convert(mod, latest),
            Visibility = mod.Visibility,
            CanUpload = user.CanUpload(mod),
            CanModerate = user.CanModerate(mod)
        };
    }

    public FileVersionDetails Convert(Mod mod, FileVersion ver)
    {
        List<FileVersionDetails.Dependency> HandleDependencies(string dependencies)
        {
            return (from dependencyId in dependencies.SSplit().Select(n => n.Trim())
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
            FileDownloadName = mod.Name.SwapWhitespace().TrimInvalid() + "_v" + ver.Version + ".zip",
            File = ver.File,
            FileSize = ver.FileSize.BytesToString(),
            GameVersions = ver.GameVersions,
            SHA256 = ver.SHA256,
            Status = ver.Status,
            UploadDate = ver.UploadDate.FormatDate(),
            Version = ver.Version
        };
    }
}
