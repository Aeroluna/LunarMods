using System.ComponentModel.DataAnnotations;

namespace LunarMods.Models;

public class AdminInput : IValidatableObject
{
    public string? ReturnMessage { get; set; }

    public string? NewGameVersion { get; init; }

    public int? NewGameVersionAlpha { get; init; }

    public ulong? TargetUser { get; init; }

    public string? SetRoles { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(NewGameVersion) && !Version.TryParse(NewGameVersion, out _))
        {
            yield return new ValidationResult(
                $"{NewGameVersion} is not a valid version.",
                new[] {nameof(NewGameVersion)});
        }
    }
}
