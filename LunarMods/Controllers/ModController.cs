using System.Net;
using System.Security.Claims;
using LunarMods.Data;
using LunarMods.Models;
using LunarMods.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LunarMods.Controllers;

public class ModController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ModController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<string> UploadImage(IFormFile file, string name)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        string wwwRootPath = _hostEnvironment.WebRootPath;
        string directory = wwwRootPath + $"/content/{name.TrimInvalid()}";
        string fileName = "preview_image".AppendId() + ".jpg";
        using Image image = await Image.LoadAsync(file.OpenReadStream());
        ResizeOptions options = new()
        {
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center,
            Size = new Size(660, 200)
        };
        image.Mutate(x => x.Resize(options));
        Directory.CreateDirectory(directory);
        await image.SaveAsJpegAsync(directory + @"/" + fileName);
        return fileName;
    }

    [Route("mod")]
    public async Task<IActionResult> Index()
    {
        List<ModDetails> details = new();
        foreach (Mod mod in await _context.Mods.ToListAsync())
        {
            switch ((Visibility)mod.Visibility)
            {
                case Visibility.Deleted:
                case Visibility.Unlisted or Visibility.Private when !User.CanModerate(mod):
                    continue;
                default:
                    details.Add(await _context.Convert(User, mod));
                    break;
            }
        }

        return View(details.OrderByDescending(n => n.LatestVersion?.Status ?? -1).ThenBy(n => n.Category).ThenBy(n => n.Name));
    }

    [Route("mod/create")]
    [Authorize(Roles = "uploader")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Route("mod/create")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "uploader")]
    public async Task<IActionResult> Create(ModInput modInput)
    {
        if (modInput.PreviewImage == null)
        {
            ModelState.AddModelError(nameof(ModInput.PreviewImage), "Preview image required.");
        }

        if (!ModelState.IsValid) return View(modInput);

        string id = Guid.NewGuid().ToString("N")[..8];
        ulong author = ulong.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("Could not get author."));
        Mod mod =  new()
        {
            Id = id,
            Name = modInput.Name,
            Author = author,
            Overview = modInput.Overview,
            Category = modInput.Category,
            PreviewImage = await UploadImage(modInput.PreviewImage!, modInput.Name),
            Description = modInput.Description,
            Repository = modInput.Repository ?? string.Empty,
            CreatedDate = StringUtil.NowDate(),
            LastUpdateDate = StringUtil.NowDate(),
            Visibility = modInput.Visibility
        };

        _context.Mods.Add(mod);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new {id = mod.Id});
    }

    [Route("mod/{id:length(8)}/edit")]
    [Authorize(Roles = "uploader, moderator")]
    public async Task<IActionResult> Edit(string id)
    {
        Mod? mod = await _context.GetMod(id);
        if (mod == null || !User.CanModerate(mod))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        ModInput modInput = new()
        {
            Id = id,
            Name = mod.Name,
            Overview = mod.Overview,
            Category = mod.Category,
            Description = mod.Description,
            Repository = mod.Repository
        };

        return View(modInput);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("mod/{id:length(8)}/edit")]
    [Authorize(Roles = "uploader, moderator")]
    public async Task<IActionResult> Edit(string id, ModInput modInput)
    {
        Mod? mod = await _context.GetMod(id);
        if (mod == null || !User.CanModerate(mod))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        if (!ModelState.IsValid) return View(modInput);

        mod.Overview = modInput.Overview;
        mod.Category = modInput.Category;
        mod.Description = modInput.Description;
        mod.Repository = modInput.Repository ?? string.Empty;
        mod.LastUpdateDate = StringUtil.NowDate();

        if (modInput.PreviewImage != null)
        {
            mod.PreviewImage = await UploadImage(modInput.PreviewImage, mod.Name);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new {id});
    }

    [HttpPost]
    [Route("mod/{id:length(8)}/delete")]
    [Authorize(Roles = "uploader, moderator")]
    public async Task<IActionResult> Delete(string id)
    {
        Mod? mod = await _context.GetMod(id);
        if (mod == null || !User.CanModerate(mod))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        mod.Visibility = (int)Visibility.Deleted;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> VerifyName(string name)
    {
        if (await _context.Mods.AnyAsync(n => n.Name == name))
        {
            return Json($"Name {name} is already in use.");
        }

        return Json(true);
    }

    [Route("mod/{id:length(8)}")]
    public async Task<IActionResult> Details(string id)
    {
        Mod? mod = await _context.GetMod(id);
        if (mod == null || ((Visibility)mod.Visibility == Visibility.Private && !User.CanModerate(mod)))
        {
            return View("StatusCode", HttpStatusCode.NotFound);
        }

        return View(new AllFileVersionDetails
        {
            Mod = await _context.Convert(User, mod)
        });
    }
}
