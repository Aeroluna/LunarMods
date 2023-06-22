using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace LunarMods.Controllers;

public class AuthController : Controller
{
    private string PreviousOrHome()
    {
        string result = Request.Headers["Referer"].ToString();
        if (string.IsNullOrEmpty(result))
        {
            result = Request.PathBase + "/home/index";
        }

        return result;
    }

    [HttpGet]
    public IActionResult Login()
    {
        string redirect = PreviousOrHome();

        if (User.Identity?.IsAuthenticated ?? false)
        {
            return Redirect(redirect);
        }

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = redirect
        });

        //return HttpContext.ChallengeAsync(DiscordAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Redirect(PreviousOrHome());
    }

    [HttpGet]
    [Route("auth/unauthorized")]
    public IActionResult UnauthorizedResponse()
    {
        return View("StatusCode", HttpStatusCode.Unauthorized);
    }

    /*[HttpGet("token")]
    [Authorize(AuthenticationSchemes = DiscordAuthenticationDefaults.AuthenticationScheme)]
    public object GenerateToken()
    {
        string userId = User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        string issuer = _config.GetValue<string>("Jwt:Issuer");
        string audience = _config.GetValue<string>("Jwt:Audience");
        string key = _config.GetValue<string>("Jwt:Key");

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> permClaims = new()
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("discordId", userId)
        };

        JwtSecurityToken token = new(
            issuer,
            audience,
            permClaims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: credentials);

        string? jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new
        {
            ApiToken = jwtToken
        };
    }*/
}
