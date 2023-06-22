using LunarMods.Data;
using LunarMods.Json;
using LunarMods.Models;
using LunarMods.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LunarMods.Controllers;

[ApiController]
[Route("api")]
public class ApiController : Controller
{
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ApplicationDbContext _context;

    public ApiController(IWebHostEnvironment hostEnvironment, ApplicationDbContext context)
    {
        _hostEnvironment = hostEnvironment;
        _context = context;
    }

    [HttpPost]
    [Route("markdown")]
    public string Markdown([FromForm] string content)
    {
        return Westwind.AspNetCore.Markdown.Markdown.Parse(content);
    }

    [HttpPost]
    [Route("uploadimage")]
    [Authorize(Roles = "uploader")]
    public async Task<string> Uploadimage(IFormCollection files)
    {
        List<string> paths = new();
        foreach (IFormFile file in files.Files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file.FileName)[..8].TrimInvalid().AppendId() + ".jpg";
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string directory = wwwRootPath + @"/content/markdown";
            using Image image = await Image.LoadAsync(file.OpenReadStream());
            Directory.CreateDirectory(directory);
            string fullPath = directory + @"/" + fileName;
            await image.SaveAsJpegAsync(fullPath);
            paths.Add(@"/content/markdown" + @"/" + fileName);
        }

        string url = HttpContext.Request.GetBaseUrl();
        string result = "[\"" + string.Join("\", \"", paths.Select(n => url + n)) + "\"]";
        return result.JsonString();
    }

    [HttpGet]
    [Route("v1/gameVersion")]
    public async Task<IActionResult> GameVersionList()
    {
        return new JsonResult((await _context.GameVersions.ToListAsync()).Select(n => new GameVersionJson(n.Version, n.Alpha)));
    }

    [HttpGet]
    [Route("v1/mod")]
    public async Task<IActionResult> ModList(string? status, string? gameVersion)
    {
        List<FileVersion> allFileVersions = await _context.FileVersions.ToListAsync();
        string[]? queriedVersion = gameVersion?.SSplit().ToArray();
        int[]? queriedStatus = status?.SSplit().Select(int.Parse).ToArray();

        List<ModJson> result = new();

        foreach (Mod mod in (await _context.Mods.ToListAsync()).Where(n =>(Visibility)n.Visibility == Visibility.Public))
        {
            User? author = await _context.Users.FindAsync(mod.Author);
            UserJson authorJson = author != null ? new UserJson(author.Id, author.Username) : new UserJson(0, "N/A");

            IEnumerable<FileVersion> allModVersions = _context.GetModVersions(mod).Where(n =>
            {
                if (queriedStatus != null && !queriedStatus.Contains(n.Status))
                {
                    return false;
                }

                return queriedVersion == null || queriedVersion.Any(n.GameVersions.Contains);
            });

            FileVersion? latest = ApplicationDbContext.GetLatestVersion(allModVersions);

            if (latest == null)
            {
                continue;
            }

            List<string> ResolveDependencies(string dependencies)
            {
                return (from dependencyId in dependencies.SSplit()
                    let dependency = _context.Mods.Find(dependencyId)
                    where dependency != null
                    select dependency.Name).ToList();
            }

            List<VersionJson> fileVersions = allFileVersions
                .Select(k => new VersionJson(
                    k.Version,
                    k.Alpha,
                    ResolveDependencies(k.Dependencies),
                    ResolveDependencies(k.Conflicts),
                    k.Status,
                    k.UploadDate,
                    $"/content/{mod.Name.TrimInvalid()}/{k.FileName}",
                    k.FileSize,
                    k.Changelog,
                    k.GameVersions.SSplit().ToList(),
                    k.MD5,
                    k.SHA256,
                    k.Files.Split('\n'))).ToList();

            result.Add(new ModJson(
                mod.Id,
                mod.Name,
                authorJson,
                mod.Overview,
                mod.Category,
                mod.Repository,
                mod.CreatedDate,
                mod.LastUpdateDate,
                latest.Version,
                fileVersions));
        }

        return new JsonResult(result);
    }
}
