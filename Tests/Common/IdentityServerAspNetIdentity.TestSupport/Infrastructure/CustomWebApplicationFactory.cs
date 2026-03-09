using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Stores;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace IdentityServerAspNetIdentity.TestSupport.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var typesToRemove = new HashSet<Type>
            {
                typeof(ApplicationDbContext),
                typeof(ConfigurationDbContext),
                typeof(PersistedGrantDbContext),
                typeof(DbContextOptions),
                typeof(DbContextOptions<ApplicationDbContext>),
                typeof(DbContextOptions<ConfigurationDbContext>),
                typeof(DbContextOptions<PersistedGrantDbContext>),
            };

            var descriptorsToRemove = services
                .Where(d =>
                {
                    if (typesToRemove.Contains(d.ServiceType))
                    {
                        return true;
                    }

                    if (!d.ServiceType.IsGenericType)
                    {
                        return false;
                    }

                    var genericDef = d.ServiceType.GetGenericTypeDefinition();
                    return genericDef == typeof(DbContextOptions<>)
                        || genericDef.FullName?.Contains("IDbContextOptionsConfiguration") == true;
                })
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            var configStoreDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(ConfigurationStoreOptions));
            if (configStoreDescriptor is not null)
            {
                services.Remove(configStoreDescriptor);
            }

            var opStoreDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(OperationalStoreOptions));
            if (opStoreDescriptor is not null)
            {
                services.Remove(opStoreDescriptor);
            }

            services.AddSingleton(new ConfigurationStoreOptions
            {
                ConfigureDbContext = db => db.UseInMemoryDatabase($"TestConfigDb-{_dbName}")
            });

            services.AddSingleton(new OperationalStoreOptions
            {
                ConfigureDbContext = db => db.UseInMemoryDatabase($"TestGrantDb-{_dbName}")
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase($"TestAppDb-{_dbName}"));

            services.AddDbContext<ConfigurationDbContext>(options =>
                options.UseInMemoryDatabase($"TestConfigDb-{_dbName}"));

            services.AddDbContext<PersistedGrantDbContext>(options =>
                options.UseInMemoryDatabase($"TestGrantDb-{_dbName}"));

            services.AddRazorPages(options =>
            {
                options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
            });

            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
                options.DefaultScheme = "TestScheme";
            });

            services.AddSingleton<IServerSideSessionStore, InMemoryServerSideSessionStore>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        appDb.Database.EnsureCreated();

        return host;
    }
}
