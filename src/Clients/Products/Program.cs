using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewProducts", policy => policy.RequireClaim("canViewProducts"));
    options.AddPolicy("CanAmendProducts", policy => policy.RequireClaim("canAmendProduct"));
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"] ?? "https://localhost:5001";

        options.ClientId = builder.Configuration["Oidc:ClientId"] ?? "products";
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"] ?? throw new InvalidOperationException("Oidc:ClientSecret must be configured via user-secrets or environment variables.");
        options.ResponseType = "code";
        options.UsePkce = true;

        options.SaveTokens = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("offline_access");
        options.Scope.Add("api1");
        options.Scope.Add("products");

        options.GetClaimsFromUserInfoEndpoint = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

app.Run();
