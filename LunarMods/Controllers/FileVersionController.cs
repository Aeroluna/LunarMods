using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using LunarMods.Data;
using LunarMods.Models;
using LunarMods.Services;
using LunarMods.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LunarMods.Controllers;

public class FileVersionController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly GameVersionService _gameVersionService;

    public FileVersionController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, GameVersionService gameVersionService)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _gameVersionService = gameVersionService;
    }

    public void CompareLatestVersion(FileVersion fileVersion, Mod mod)
    {
        if (!_context.CompareLatestVersion(fileVersion, mod))
        {
            return;
        }

        mod.LatestVersion = fileVersion.Id;
        mod.LastUpdateDate = StringUtil.NowDate();
    }

    public void UpdateLatestVersion(FileVersion fileVersion, Mod mod)
    {
        FileVersion? maxVersion = ApplicationDbContext.GetLatestVersion(_context.GetModVersions(mod).Where(n => n != fileVersion));

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
        foreach (string dependencyName in dependencies.SSplit())
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
            .Select(n => _context.Convert(n))
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

        if (!_gameVersionService.ValidateGameVersions(input))
        {
            ModelState.AddModelError(nameof(FileVersionInput.GameVersions), $"{input.GameVersions} is invalid.");
        }

        if (input.File == null)
        {
            ModelState.AddModelError(nameof(FileVersionInput.File), $"{nameof(input.File)} required.");
        }

        if (!Version.TryParse(input.Version, out Version? versionVersion))
        {
            ModelState.AddModelError(nameof(FileVersionInput.Version), $"{versionVersion} is not a valid version.");
        }

        string version = versionVersion?.ToString() ?? string.Empty;
        if (await _context.FileVersions.AnyAsync(n => n.Version == version))
        {
            ModelState.AddModelError(nameof(FileVersionInput.Version), $"Version {version} already exists.");
        }

        string dependencies = await ValidateDependencies(input.Dependencies);
        string conflicts = await ValidateDependencies(input.Conflicts);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        string wwwRootPath = _hostEnvironment.WebRootPath;
        string directory = wwwRootPath.JsonString() + $"/content/{mod.Name.TrimInvalid()}";
        string fileName = mod.Name.TrimInvalid() + "_v" + version + ".zip";
        Directory.CreateDirectory(directory);

        List<string> entries = new();
        await using Stream read = input.File!.OpenReadStream();
        await using MemoryStream zipStream = new();
        await read.CopyToAsync(zipStream);
        using (ZipArchive zip = new(zipStream, ZipArchiveMode.Update, true))
        {
            entries.AddRange(zip.Entries.Select(zipArchiveEntry => zipArchiveEntry.ToString()));
        }

        // read metadata
        zipStream.Position = 0;
        long size = zipStream.Length;
        MD5 md5 = MD5.Create();
        SHA256 sha256 = SHA256.Create();
        string md5Hash = BitConverter.ToString(await md5.ComputeHashAsync(zipStream)).Replace("-", string.Empty).ToLowerInvariant();
        zipStream.Position = 0;
        string sha256Hash = BitConverter.ToString(await sha256.ComputeHashAsync(zipStream)).Replace("-", string.Empty).ToLowerInvariant();
        zipStream.Position = 0;
        await using (FileStream write = System.IO.File.OpenWrite(directory + @"/" + fileName))
        {
            await zipStream.CopyToAsync(write);
        }

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
            FileName = fileName,
            FileSize = size,
            MD5 = md5Hash,
            SHA256 = sha256Hash,
            Files = string.Join('\n', entries)
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
            foreach (string dependencyId in dependencies.SSplit())
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

        if (!_gameVersionService.ValidateGameVersions(input))
        {
            ModelState.AddModelError(nameof(FileVersionInput.GameVersions), $"{input.GameVersions} is invalid.");
        }

        string dependencies = await ValidateDependencies(input.Dependencies);
        string conflicts = await ValidateDependencies(input.Conflicts);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

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
        return RedirectToAction(nameof(Index), "FileVersion", new {id = mod.Id});
    }

    [HttpPost]
    [Route("mod/{modId:length(8)}/files/{id:length(8)}/approve")]
    [Authorize(Roles = "approver")]
    public async Task<IActionResult> Approve(string modId, string id)
    {
        Mod? mod = await _context.GetMod(modId);
        FileVersion? fileVersion = await _context.FileVersions.FindAsync(id);
        if (mod == null || fileVersion == null)
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        fileVersion.Status = (int)Status.Approved;
        CompareLatestVersion(fileVersion, mod);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), "FileVersion", new {id = mod.Id});
    }
}
