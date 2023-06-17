using System.ComponentModel.DataAnnotations;
using LunarMods.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LunarMods.Models;

public class FileVersionInput : IValidatableObject
{
    public string Mod { get; init; }

    public string ModName { get; init; }

    [Display(Name="Release Type")]
    public int Alpha { get; init; }

    public string? Dependencies { get; init; }

    public string? Conflicts { get; init; }

    public string Version { get; init; }

    public IFormFile? File { get; init; }

    public string? Changelog { get; init; }

    [Display(Name="Compatible Game Versions")]
    public string GameVersions { get; init; }

    public static IEnumerable<string> AllVersions { get; } = new[]
    {
        "1.30.2",
    };

    public static SelectList AlphaOptions { get; } = new(Enum.GetValues<Alpha>()
        .Select(n => new { Value = (int)n, Text = n.ToString() }), "Value", "Text");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!System.Version.TryParse(Version, out _))
        {
            yield return new ValidationResult(
                $"{Version} is not a valid version.",
                new[] {nameof(Version)});
        }

        foreach (string gameVersion in GameVersions.SSplit().Select(n => n.Trim()).Where(n => !AllVersions.Contains(n)))
        {
            yield return new ValidationResult(
                $"{gameVersion} is not a valid version.",
                new[] {nameof(GameVersions)});
        }

        if (!Enum.IsDefined(typeof(Alpha), Alpha))
        {
            yield return new ValidationResult(
                $"{Alpha} is not a valid alpha status.",
                new[] {nameof(Alpha) });
        }
    }
}
