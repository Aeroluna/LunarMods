using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LunarMods.Models;

namespace LunarMods.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Route("/privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [Route("/terms")]
    public IActionResult Terms()
    {
        return View();
    }

    [Route("/disclaimer")]
    public IActionResult Disclaimer()
    {
        return View();
    }

    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        //return Problem();
        return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}
