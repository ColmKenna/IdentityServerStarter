using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Stores;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace IdentityServerAspNetIdentity.E2ETests.Infrastructure;

public class PlaywrightWebApplicationFactory : CustomWebApplicationFactory
{
    private readonly string _databaseDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "playwright-dbs",
        Guid.NewGuid().ToString("N"));
    private readonly int _port = GetFreePort();

    private IHost? _kestrelHost;

    public string RootUrl => ClientOptions.BaseAddress?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("The browser host has not been started.");

    private string ApplicationDbPath => Path.Combine(_databaseDirectory, "app.db");
    private string ConfigurationDbPath => Path.Combine(_databaseDirectory, "config.db");
    private string PersistedGrantDbPath => Path.Combine(_databaseDirectory, "grant.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            Directory.CreateDirectory(_databaseDirectory);

            var typesToRemove = new HashSet<Type>
            {
                typeof(ApplicationDbContext),
                typeof(ConfigurationDbContext),
                typeof(PersistedGrantDbContext),
                typeof(DbContextOptions),
                typeof(DbContextOptions<ApplicationDbContext>),
                typeof(DbContextOptions<ConfigurationDbContext>),
                typeof(DbContextOptions<PersistedGrantDbContext>)
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

                    var genericTypeDefinition = d.ServiceType.GetGenericTypeDefinition();
                    return genericTypeDefinition == typeof(DbContextOptions<>)
                        || genericTypeDefinition.FullName?.Contains("IDbContextOptionsConfiguration") == true;
                })
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            var configStoreDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ConfigurationStoreOptions));
            if (configStoreDescriptor is not null)
            {
                services.Remove(configStoreDescriptor);
            }

            var opStoreDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(OperationalStoreOptions));
            if (opStoreDescriptor is not null)
            {
                services.Remove(opStoreDescriptor);
            }

            services.AddSingleton(new ConfigurationStoreOptions
            {
                ConfigureDbContext = db => db.UseSqlite($"Data Source={ConfigurationDbPath}")
            });

            services.AddSingleton(new OperationalStoreOptions
            {
                ConfigureDbContext = db => db.UseSqlite($"Data Source={PersistedGrantDbPath}")
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={ApplicationDbPath}"));

            services.AddDbContext<ConfigurationDbContext>(options =>
                options.UseSqlite($"Data Source={ConfigurationDbPath}"));

            services.AddDbContext<PersistedGrantDbContext>(options =>
                options.UseSqlite($"Data Source={PersistedGrantDbPath}"));

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

            services.AddSingleton<IServerSideSessionStore, TestSupport.Infrastructure.InMemoryServerSideSessionStore>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        testHost.Start();
        EnsureDatabasesCreated(testHost.Services);

        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls($"http://127.0.0.1:{_port}");
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();
        EnsureDatabasesCreated(_kestrelHost.Services);
        ClientOptions.BaseAddress = new Uri($"http://127.0.0.1:{_port}");
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.Dispose();
        }

        base.Dispose(disposing);

        if (disposing && Directory.Exists(_databaseDirectory))
        {
            try
            {
                Directory.Delete(_databaseDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void EnsureDatabasesCreated(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.EnsureCreated();
    }

    private static int GetFreePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }
}
