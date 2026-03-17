using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityModel;
using IdentityServer.EF.DataAccess;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using IdentityServerAspNetIdentity.Pages.Components;
using Duende.IdentityServer.Models;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerServices;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace IdentityServerAspNetIdentity;

internal static class HostingExtensions
{
    //private const string DefaultConnectionDocker = "DefaultConnection";
    private const string DefaultConnectionDocker = "DefaultConnectionDocker";

    private static async Task InitializeDatabaseAsync(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var persistedGrantDbContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
        await persistedGrantDbContext.Database.MigrateAsync();

        var configurationDbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        await configurationDbContext.Database.MigrateAsync();

        var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await applicationDbContext.Database.MigrateAsync();

        var userMgr = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var alice = await userMgr.FindByNameAsync("alice");
        if (alice == null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
                FavoriteColor = "red",
            };
            var result = await userMgr.CreateAsync(alice, "Pass123$");
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = await userMgr.AddClaimsAsync(alice, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, "Alice Smith"),
                new Claim(JwtClaimTypes.GivenName, "Alice"),
                new Claim(JwtClaimTypes.FamilyName, "Smith"),
                new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                new Claim("canViewProducts", "true"),
                new Claim("canAmendProduct", "true"),
            });
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            Log.Debug("alice created");
        }
        else
        {
            Log.Debug("alice already exists");
        }

        var bob = await userMgr.FindByNameAsync("bob");
        if (bob == null)
        {
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true
            };
            var result = await userMgr.CreateAsync(bob, "Pass123$");
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = await userMgr.AddClaimsAsync(bob, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, "Bob Smith"),
                new Claim(JwtClaimTypes.GivenName, "Bob"),
                new Claim(JwtClaimTypes.FamilyName, "Smith"),
                new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                new Claim("location", "somewhere"),
                new Claim("canViewProducts", "true"),
                new Claim("canAmendProduct", "true"),
            });
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            Log.Debug("bob created");
        }
        else
        {
            Log.Debug("bob already exists");
        }

        var charlie = await userMgr.FindByNameAsync("charlie");
        if (charlie == null)
        {
            charlie = new ApplicationUser
            {
                UserName = "charlie",
                Email = "CharlieDay@email.com",
                EmailConfirmed = true
            };
            var result = await userMgr.CreateAsync(charlie, "Pass123$");
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = await userMgr.AddClaimsAsync(charlie, new Claim[]
            {
                new Claim(JwtClaimTypes.Name, "Charlie Day"),
                new Claim(JwtClaimTypes.GivenName, "Charlie"),
                new Claim(JwtClaimTypes.FamilyName, "Day"),
                new Claim("canViewProducts", "true"),
            });
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            Log.Debug("charlie created");
        }
        else
        {
            Log.Debug("charlie already exists");
        }

        if (!await configurationDbContext.Clients.AnyAsync())
        {
            foreach (var client in Config.Clients)
            {
                configurationDbContext.Clients.Add(client.ToEntity());
            }

            await configurationDbContext.SaveChangesAsync();
        }

        if (!await configurationDbContext.IdentityResources.AnyAsync())
        {
            foreach (var resource in Config.IdentityResources)
            {
                configurationDbContext.IdentityResources.Add(resource.ToEntity());
            }

            await configurationDbContext.SaveChangesAsync();
        }

        if (!await configurationDbContext.ApiScopes.AnyAsync())
        {
            foreach (var resource in Config.ApiScopes)
            {
                configurationDbContext.ApiScopes.Add(resource.ToEntity());
            }

            await configurationDbContext.SaveChangesAsync();
        }
    }


    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var migrationsAssembly = typeof(ApplicationDbContext).Assembly.GetName().Name;

        builder.Services.AddRazorPages();

        builder.Services.AddAuthorization(options =>
        {
            // ── Users ─────────────────────────────────────────────────────
            options.AddPolicy(UserPolicyConstants.UsersRead,   p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUsers));
            options.AddPolicy(UserPolicyConstants.UsersWrite,  p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUsers));
            options.AddPolicy(UserPolicyConstants.UsersDelete, p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUsers));

            // ── User Claims ───────────────────────────────────────────────
            options.AddPolicy(UserPolicyConstants.UserClaimsRead,   p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserClaims));
            options.AddPolicy(UserPolicyConstants.UserClaimsWrite,  p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserClaims));
            options.AddPolicy(UserPolicyConstants.UserClaimsDelete, p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserClaims));

            // ── User Roles ─────────────────────────────────────────────────
            options.AddPolicy(UserPolicyConstants.UserRolesRead,   p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserRoles));
            options.AddPolicy(UserPolicyConstants.UserRolesWrite,  p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserRoles));
            options.AddPolicy(UserPolicyConstants.UserRolesDelete, p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserRoles));

            // ── Grants ─────────────────────────────────────────────────────
            options.AddPolicy(UserPolicyConstants.UserGrantsRead,   p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserGrants));
            options.AddPolicy(UserPolicyConstants.UserGrantsDelete, p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserGrants));

            // ── Sessions ───────────────────────────────────────────────────
            options.AddPolicy(UserPolicyConstants.UserSessionsRead,   p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserSessions));
            options.AddPolicy(UserPolicyConstants.UserSessionsDelete, p => p.RequireClaim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserSessions));
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString(DefaultConnectionDocker)));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;
            })
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(builder.Configuration.GetConnectionString(DefaultConnectionDocker),
                        sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(builder.Configuration.GetConnectionString(DefaultConnectionDocker),
                        sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddProfileService<CustomProfileService>();

        builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                builder.Configuration.GetSection("Authentication:Google").Bind(options);
            });

        builder.Services.AddScoped<IScriptHolder,ScriptHolder>();
        builder.Services.AddScoped<IEmailSender, EmailSenderConsole>();
        builder.Services.AddScoped<IApiScopesAdminService, ApiScopesAdminService>();
        builder.Services.AddScoped<IClaimsAdminService, ClaimsAdminService>();
        builder.Services.AddScoped<IClientAdminService, ClientAdminService>();
        builder.Services.AddScoped<IIdentityResourcesAdminService, IdentityResourcesAdminService>();
        builder.Services.AddScoped<IRolesAdminService, RolesAdminService>();
        builder.Services.AddScoped<IUserEditor, UserEditor>();

        return builder.Build();
    }

    public static async Task<WebApplication> ConfigurePipelineAsync(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            await InitializeDatabaseAsync(app);
        }
        // Add a Content-Security-Policy header allowing styles from cdnjs.cloudflare.com
        app.UseMiddleware<Middleware.ContentSecurityPolicyMiddleware>();

        // Route alias: serve create mode from the Edit Razor Page without a redirect.
        app.UseMiddleware<Middleware.ApiScopesRouteAliasMiddleware>();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        
        app.UseAuthorization();
        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}
