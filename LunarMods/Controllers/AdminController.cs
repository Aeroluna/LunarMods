using LunarMods.Data;
using LunarMods.Models;
using LunarMods.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunarMods.Controllers;

[Route("admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly GameVersionService _gameVersionService;

    public AdminController(ApplicationDbContext context, GameVersionService gameVersionService)
    {
        _context = context;
        _gameVersionService = gameVersionService;
    }

    [Route("")]
    [Authorize(Roles = "admin")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [Route("submitgameversion")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SubmitGameVersion(AdminInput input)
    {
        if (string.IsNullOrEmpty(input.NewGameVersion))
        {
            ModelState.AddModelError(nameof(AdminInput.NewGameVersion), $"{input.NewGameVersion} is invalid.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", input);
        }

        GameVersion? gameVersion = await _context.GameVersions.FindAsync(input.NewGameVersion);
        int alpha = Math.Clamp(input.NewGameVersionAlpha ?? (int)Alpha.Release, 0, 1);
        if (gameVersion != null)
        {
            input.ReturnMessage = $"Set {input.NewGameVersion} to {(Alpha)alpha}";
            gameVersion.Alpha = alpha;
            await _context.SaveChangesAsync();
        }
        else
        {
            gameVersion = new GameVersion
            {
                Version = input.NewGameVersion!,
                Alpha = alpha
            };

            input.ReturnMessage = $"Created new {input.NewGameVersion} with alpha {(Alpha)alpha}";
            await _context.GameVersions.AddAsync(gameVersion);
            await _context.SaveChangesAsync();
        }

        _gameVersionService.Refresh();
        return View("Index", input);
    }

    [HttpPost]
    [Route("refreshgameversion")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public IActionResult RefreshGameVersion(AdminInput input)
    {
        input.ReturnMessage = "Refreshed game versions";
        _gameVersionService.Refresh();
        return View("Index", input);
    }

    [HttpPost]
    [Route("setuserroles")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetUserRoles(AdminInput input)
    {
        if (input.TargetUser == null)
        {
            ModelState.AddModelError(nameof(AdminInput.TargetUser), $"{input.TargetUser} is invalid.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", input);
        }

        User? user = await _context.Users.FindAsync(input.TargetUser);
        string roles = input.SetRoles ?? string.Empty;
        if (user != null)
        {
            input.ReturnMessage = $"Set user {user.Username} with id {user.Id} to roles {roles}";
            user.Roles = roles;
            await _context.SaveChangesAsync();
        }
        else
        {
            user = new User
            {
                Id = input.TargetUser!.Value,
                Roles = roles,
                Username = string.Empty
            };

            input.ReturnMessage = $"Created new user {user.Id} with roles {roles}";
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        return View("Index", input);
    }
}
