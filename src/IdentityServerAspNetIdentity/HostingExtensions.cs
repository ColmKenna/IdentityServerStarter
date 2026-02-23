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
    private const string DefaultConnectionDocker = "DefaultConnection";
    ///private const string DefaultConnectionDocker = "DefaultConnectionDocker";

    private static void InitializeDatabase(IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
        {
            var persistedGrantDbContext = serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
            persistedGrantDbContext.Database.Migrate();

            var configurationDbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            configurationDbContext.Database.Migrate();

            var applicationDbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            applicationDbContext.Database.Migrate();

            var userMgr = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var alice = userMgr.FindByNameAsync("alice").Result;
            if (alice == null)
            {
                alice = new ApplicationUser
                {
                    UserName = "alice",
                    Email = "AliceSmith@email.com",
                    EmailConfirmed = true,
                    FavoriteColor = "red",
                };
                var result = userMgr.CreateAsync(alice, "Pass123$").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddClaimsAsync(alice, new Claim[]
                {
                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                }).Result;
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

            var bob = userMgr.FindByNameAsync("bob").Result;
            if (bob == null)
            {
                bob = new ApplicationUser
                {
                    UserName = "bob",
                    Email = "BobSmith@email.com",
                    EmailConfirmed = true
                };
                var result = userMgr.CreateAsync(bob, "Pass123$").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddClaimsAsync(bob, new Claim[]
                {
                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                    new Claim("location", "somewhere")
                }).Result;
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


            if (!configurationDbContext.Clients.Any())
            {
                foreach (var client in Config.Clients)
                {
                    configurationDbContext.Clients.Add(client.ToEntity());
                }

                configurationDbContext.SaveChanges();
            }

            if (!configurationDbContext.IdentityResources.Any())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    configurationDbContext.IdentityResources.Add(resource.ToEntity());
                }

                configurationDbContext.SaveChanges();
            }

            if (!configurationDbContext.ApiScopes.Any())
            {
                foreach (var resource in Config.ApiScopes)
                {
                    configurationDbContext.ApiScopes.Add(resource.ToEntity());
                }

                configurationDbContext.SaveChanges();
            }
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

                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            });

        builder.Services.AddScoped<IScriptHolder,ScriptHolder>();
        builder.Services.AddScoped<IEmailSender, EmailSenderConsole>();
        builder.Services.AddScoped<IClientEditor, ClientEditor>();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }


        InitializeDatabase(app);
        // Add a Content-Security-Policy header allowing styles from cdnjs.cloudflare.com
        app.Use(async (context, next) =>
        {
            // Keep a restrictive default-src to 'self' and allow styles from the CDN used for Font Awesome
            var csp = "default-src 'self'; style-src 'self' https://cdnjs.cloudflare.com; script-src 'self' https://cdnjs.cloudflare.com; font-src 'self' https://cdnjs.cloudflare.com;";
            context.Response.Headers["Content-Security-Policy"] = csp;
            await next();
        });

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        
        app.UseAuthorization();
        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}
