using AuthServer.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
                .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options
        .SetAuthorizationEndpointUris("connect/authorize")
        .SetEndSessionEndpointUris("connect/logout") //logout in the previous version
        .SetTokenEndpointUris("connect/token")
        .SetUserInfoEndpointUris("connect/userinfo");

        options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

        options.AllowAuthorizationCodeFlow();

        options.AddEncryptionKey(new SymmetricSecurityKey(
            Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));


        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            //TBD
        }

        options
        .UseAspNetCore()
        .EnableAuthorizationEndpointPassthrough()
        .EnableEndSessionEndpointPassthrough() //logout in the previous version
        .EnableTokenEndpointPassthrough()
        .EnableUserInfoEndpointPassthrough();

        options.DisableAccessTokenEncryption();

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(60));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
        options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(10));
        options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(60));
    })
    .AddValidation(options =>
    {
        options.UseLocalServer(); //TODO Check this config
        options.UseAspNetCore();
    }
    );

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(c =>
    {
        c.LoginPath = "/Authenticate";
    });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
