using LunarMods.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunarMods.Controllers;

[ApiController]
[Route("api")]
public class ApiController : Controller
{
    private readonly IWebHostEnvironment _hostEnvironment;

    public ApiController(IWebHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    [HttpPost]
    [Route("markdown")]
    public string Markdown([FromForm] string content)
    {
        return Westwind.AspNetCore.Markdown.Markdown.Parse(content);
    }

    [HttpPost]
    [Route("uploadimage")]
    [Authorize("uploader")]
    public async Task<string> Uploadimage(IFormCollection files)
    {
        List<string> paths = new();
        foreach (IFormFile file in files.Files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file.FileName)[..8].Transform(".jpg");
            string wwwRootPath = _hostEnvironment.WebRootPath;
            string directory = wwwRootPath + @"\content\markdown";
            using Image image = await Image.LoadAsync(file.OpenReadStream());
            Directory.CreateDirectory(directory);
            string fullPath = directory + @"\" + fileName;
            await image.SaveAsJpegAsync(fullPath);
            paths.Add(@"/content/markdown" + @"/" + fileName);
        }

        string url = HttpContext.Request.GetBaseUrl();
        string result = "[\"" + string.Join("\", \"", paths.Select(n => url + n)) + "\"]";
        return result.JsonString();
    }
}
