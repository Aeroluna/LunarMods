using System.Net;
using System.Security.Cryptography;
using LunarMods.Data;
using LunarMods.Models;
using LunarMods.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LunarMods.Controllers;

public class FileVersionController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public FileVersionController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    public void CompareLatestVersion(FileVersion fileVersion, Mod mod)
    {
        if ((Alpha)fileVersion.Alpha != Alpha.Release)
        {
            return;
        }

        if (mod.LatestVersion == null)
        {
            mod.LatestVersion = fileVersion.Id;
            mod.LastUpdateDate = StringUtil.NowDate();
            return;
        }

        FileVersion? currentLatest = _context.FileVersions.Find(mod.LatestVersion);
        if (currentLatest == null)
        {
            mod.LatestVersion = fileVersion.Id;
            mod.LastUpdateDate = StringUtil.NowDate();
            return;
        }

        Version currentVersion = Version.Parse(currentLatest.Version);
        Version newVersion = Version.Parse(fileVersion.Version);
        if (newVersion.CompareTo(currentVersion) <= 0)
        {
            return;
        }

        mod.LatestVersion = fileVersion.Id;
        mod.LastUpdateDate = StringUtil.NowDate();
    }

    public void UpdateLatestVersion(FileVersion fileVersion, Mod mod)
    {
        FileVersion? maxVersion = null;
        Version? versionMaxValue = null;
        foreach (FileVersion version in _context.FileVersions
                     .Where(n => n.Mod == mod.Id && (Alpha)n.Alpha == Alpha.Release && (Visibility)n.Visibility != Visibility.Deleted && n.Id != fileVersion.Id))
        {
            Version newVer = Version.Parse(version.Version);
            if (newVer.CompareTo(versionMaxValue) <= 0)
            {
                continue;
            }

            maxVersion = version;
            versionMaxValue = newVer;
        }

        if (maxVersion == null)
        {
            return;
        }

        mod.LatestVersion = maxVersion.Id;
        mod.LastUpdateDate = StringUtil.NowDate();
    }

    private async Task<string> ValidateDependencies(string? dependencies)
    {
        if (dependencies == null)
        {
            return string.Empty;
        }

        List<string> result = new();
        foreach (string dependencyName in dependencies.SSplit().Select(n => n.Trim()))
        {
            Mod? dependency = await _context.Mods.FirstOrDefaultAsync(n => n.Name == dependencyName);
            if (dependency == null || (Visibility)dependency.Visibility is Visibility.Deleted or Visibility.Private)
            {
                ModelState.AddModelError(nameof(FileVersionInput.Dependencies), $"Could not resolve {dependencyName}.");
                break;
            }

            result.Add(dependency.Id);
        }

        return string.Join(", ", result);
    }

    [Route("mod/{id:length(8)}/files")]
    public async Task<IActionResult> Index(string id)
    {
        Mod? mod = await _context.GetMod(id);
        if (mod == null || ((Visibility)mod.Visibility == Visibility.Private && !User.CanModerate(mod)))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        List<FileVersionDetails> fileVersions = (await _context.FileVersions.ToListAsync())
            .Where(n => (Visibility)n.Visibility != Visibility.Deleted && n.Mod == id)
            .Select(n => _context.Convert(mod, n))
            .OrderByDescending(n => Version.Parse(n.Version))
            .ToList();

        return View(new AllFileVersionDetails
        {
            Mod = await _context.Convert(User, mod),
            Versions = fileVersions
        });
    }

    [Route("mod/{id:length(8)}/upload")]
    [Authorize(Roles = "uploader")]
    public async Task<IActionResult> Upload(string id)
    {
        Mod? mod = await _context.GetMod(id);
        if (mod == null)
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        if (!User.CanUpload(mod))
        {
            return View("StatusCode", HttpStatusCode.Forbidden);
        }

        return View(new FileVersionInput
        {
            Mod = mod.Id,
            ModName = mod.Name
        });
    }

    [HttpPost]
    [Route("mod/{modId:length(8)}/upload")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "uploader")]
    public async Task<IActionResult> Upload(FileVersionInput input, string modId)
    {
        Mod? mod = await _context.GetMod(modId);
        if (mod == null)
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        if (!User.CanUpload(mod))
        {
            return View("StatusCode", HttpStatusCode.Forbidden);
        }

        if (input.File == null)
        {
            ModelState.AddModelError(nameof(FileVersionInput.File), $"{nameof(input.File)} required.");
        }

        string version = Version.Parse(input.Version).ToString();
        if (await _context.FileVersions.AnyAsync(n => n.Version == version))
        {
            ModelState.AddModelError(nameof(FileVersionInput.Version), $"Version {version} already exists.");
        }

        string dependencies = await ValidateDependencies(input.Dependencies);
        string conflicts = await ValidateDependencies(input.Conflicts);

        if (!ModelState.IsValid) return View(input);

        SHA256 sha256 = SHA256.Create();
        string wwwRootPath = _hostEnvironment.WebRootPath;
        string directory = wwwRootPath + $"\\content\\{mod.Name}";
        string fileName = version.AppendId() + ".zip";
        Directory.CreateDirectory(directory);
        await using FileStream write = System.IO.File.OpenWrite(directory + @"\" + fileName);
        await using Stream read = input.File!.OpenReadStream();
        await read.CopyToAsync(write);
        read.Position = 0;
        string hash = BitConverter.ToString(await sha256.ComputeHashAsync(read)).Replace("-", string.Empty).ToLowerInvariant();
        long size = read.Length;

        string id = Guid.NewGuid().ToString("N")[..8];
        FileVersion fileVersion = new()
        {
            Id = id,
            Mod = mod.Id,
            Alpha = input.Alpha,
            Version = version,
            Status = User.IsInRole("trusted") ? 1 : 0,
            Dependencies = dependencies,
            Conflicts = conflicts,
            UploadDate = StringUtil.NowDate(),
            Changelog = input.Changelog ?? string.Empty,
            GameVersions = input.GameVersions,
            File = fileName,
            FileSize = size,
            SHA256 = hash
        };

        _context.FileVersions.Add(fileVersion);
        CompareLatestVersion(fileVersion, mod);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), "FileVersion", new {id = mod.Id});
    }

    [Route("mod/{modId:length(8)}/files/{id:length(8)}/edit")]
    [Authorize(Roles = "uploader, moderator")]
    public async Task<IActionResult> Edit(string modId, string id)
    {
        Mod? mod = await _context.GetMod(modId);
        FileVersion? version = await _context.GetVersion(id);
        if (mod == null || version == null || !User.CanModerate(mod))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        async Task<string> ReverseDependencies(string dependencies)
        {
            List<string> result = new();
            foreach (string dependencyId in dependencies.SSplit().Select(n => n.Trim()))
            {
                Mod? dependency = await _context.GetMod(dependencyId);
                if (dependency == null)
                {
                    continue;
                }

                result.Add(dependency.Name);
            }

            return string.Join(", ", result);
        }

        return View(new FileVersionInput
        {
            Mod = mod.Id,
            ModName = mod.Name,
            Version = version.Version,
            Alpha = version.Alpha,
            Dependencies = await ReverseDependencies(version.Dependencies),
            Conflicts = await ReverseDependencies(version.Conflicts),
            Changelog = version.Changelog,
            GameVersions = version.GameVersions
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("mod/{modId:length(8)}/files/{id:length(8)}/edit")]
    [Authorize(Roles = "uploader, moderator")]
    public async Task<IActionResult> Edit(string modId, string id, FileVersionInput input)
    {
        Mod? mod = await _context.GetMod(modId);
        FileVersion? version = await _context.GetVersion(id);
        if (mod == null || version == null || !User.CanModerate(mod))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        string dependencies = await ValidateDependencies(input.Dependencies);
        string conflicts = await ValidateDependencies(input.Conflicts);

        if (!ModelState.IsValid) return View(input);

        version.Alpha = input.Alpha;
        version.Dependencies = dependencies;
        version.Conflicts = conflicts;
        version.Changelog = input.Changelog ?? string.Empty;
        version.GameVersions = input.GameVersions;

        switch ((Alpha)input.Alpha)
        {
            case Alpha.Release:
                CompareLatestVersion(version, mod);
                break;
            case Alpha.PreRelease:
                UpdateLatestVersion(version, mod);
                break;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return View("StatusCode", HttpStatusCode.InternalServerError);
        }

        return RedirectToAction(nameof(Index), "FileVersion", new {id = mod.Id});
    }

    [HttpPost]
    [Route("mod/{modId:length(8)}/files/{id:length(8)}/delete")]
    [Authorize(Roles = "uploader, moderator")]
    public async Task<IActionResult> Delete(string modId, string id)
    {
        Mod? mod = await _context.GetMod(modId);
        FileVersion? fileVersion = await _context.FileVersions.FindAsync(id);
        if (mod == null || fileVersion == null || !User.CanModerate(mod))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        fileVersion.Visibility = (int)Visibility.Deleted;
        UpdateLatestVersion(fileVersion, mod);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ModController.Details), "Mod", new {id = mod.Id});
    }
}
