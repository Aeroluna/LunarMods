using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LunarMods.Models;

public class ModInput : IValidatableObject
{
    public string? Id { get; init; }

    [Remote(action: "VerifyName", controller: "Mod")]
    public string Name { get; init; } = string.Empty;

    [StringLength(200)]
    public string Overview { get; init; } = string.Empty;

    [StringLength(50)]
    public string Category { get; init; } = string.Empty;

    [DisplayName("Preview Image")]
    public IFormFile? PreviewImage { get; init; }

    [StringLength(20000)]
    public string Description { get; init; } = string.Empty;

    [StringLength(200)]
    public string? Repository { get; init; }

    public int Visibility { get; init; }

    public static SelectList VisibilityOptions { get; } = new(Enum.GetValues<Visibility>()
        .Where(n => n != Models.Visibility.Deleted)
        .Select(n => new { Value = (int)n, Text = n.ToString() }), "Value", "Text");

    public static IEnumerable<string> CategoryNames { get; } = new[]
    {
        "Other",
        "Core",
        "Cosmetic",
        "Practice / Training",
        "Gameplay",
        "Stream Tools",
        "Libraries",
        "UI Enhancements",
        "Lighting",
        "Tweaks / Tools",
        "Multiplayer",
        "Text Changes",
        "Player Accessibility",
        "Leaderboards"
    };

    public static SelectList CategoryOptions { get; } = new(CategoryNames
        .Select(n => new SelectListItem {Text = n}), "Text", "Text");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!CategoryNames.Contains(Category))
        {
            yield return new ValidationResult(
                $"{Category} is not a valid category.",
                new[] {nameof(Category) });
        }

        if (!Enum.IsDefined(typeof(Visibility), Visibility))
        {
            yield return new ValidationResult(
                $"{Visibility} is not a valid visibility.",
                new[] {nameof(Visibility) });
        }
    }
}
