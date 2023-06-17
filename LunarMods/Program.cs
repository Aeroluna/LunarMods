using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AspNet.Security.OAuth.Discord;
using IdentityModel.AspNetCore.AccessTokenValidation;
using LunarMods.Data;
using LunarMods.Models;
using LunarMods.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ConfigurationManager config = builder.Configuration;


/*builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestHeaders | HttpLoggingFields.RequestBody | HttpLoggingFields.Response;
    options.RequestBodyLogLimit = 4096;
    options.ResponseBodyLogLimit = 4096;
    options.MediaTypeOptions.AddText("multipart/form-data");
    options.MediaTypeOptions.AddText("application/x-www-form-urlencoded");
});*/

// Add services to the container.
builder.Services.AddDbContextPool<ApplicationDbContext>(options => options
    .UseMySql(config.GetConnectionString("MariaDb"), ServerVersion.Parse("10.11.2-MariaDB")));

builder.Services.AddControllersWithViews(options =>
{
    options.RequireHttpsPermanent = true;
    options.RespectBrowserAcceptHeader = true;
});

const string accessDeniedPath = "/auth/unauthorized/";

JwtSettings jwtSettings = new();
config.Bind("Jwt", jwtSettings);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
    })
    // asp.net core authentication shit makes me want to kms
    /*.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = jwtSettings.Expire
        };
        options.SaveToken = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("X-Access-Token", out string? token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    })*/
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            config.Bind("Cookie", options);
            options.AccessDeniedPath = new PathString(accessDeniedPath);
        })
    .AddDiscord(options =>
    {
        config.Bind("Discord", options);
        options.Scope.Add("identify");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
        options.CallbackPath = new PathString("/auth/callback");
        options.AccessDeniedPath = new PathString(accessDeniedPath);
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                HttpRequestMessage request = new(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                HttpResponseMessage response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                JsonDocument userJson = await JsonDocument.ParseAsync(response.Content.ReadAsStream());

                context.RunClaimActions(userJson.RootElement);

                ulong id = ulong.Parse(userJson.RootElement.GetProperty("id").GetString() ?? throw new InvalidOperationException("No [id] found."));
                ApplicationDbContext db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                User? user = await db.Users.FirstOrDefaultAsync(n => n.Id == id);
                if (user == null)
                {
                    user = new User
                    {
                        Id = id,
                        Username = userJson.RootElement.GetProperty("username").GetString() ?? throw new InvalidOperationException("No [username] found.")
                    };
                    db.Add(user);
                    await db.SaveChangesAsync();
                }

                foreach (string s in user.Roles.SSplit())
                {
                    context.Identity?.AddClaim(new Claim(ClaimTypes.Role, s.Trim()));
                }
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

WebApplication app = builder.Build();

////app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
