namespace LunarMods.Utilities;

public class JwtSettings
{

    public string Key { get; init; }

    public string Issuer { get; init;}

    public string Audience { get; init; }

    public int MinutesToExpiration { get; init; }

    public TimeSpan Expire => TimeSpan.FromMinutes(MinutesToExpiration);
}
