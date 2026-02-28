using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Nuclear removal: remove every service whose type or implementation
            // relates to DbContext configuration to avoid dual-provider conflicts.
            // EF Core registers IDbContextOptionsConfiguration<T> services that accumulate
            // provider registrations, so we must remove those too.
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
                        return true;

                    // Remove all generic DbContextOptions<> and IDbContextOptionsConfiguration<>
                    if (d.ServiceType.IsGenericType)
                    {
                        var genericDef = d.ServiceType.GetGenericTypeDefinition();
                        if (genericDef == typeof(DbContextOptions<>))
                            return true;
                        if (genericDef.FullName?.Contains("IDbContextOptionsConfiguration") == true)
                            return true;
                    }

                    return false;
                })
                .ToList();

            foreach (var d in descriptorsToRemove)
            {
                services.Remove(d);
            }

            // Replace ConfigurationStoreOptions with one that uses InMemory
            var configStoreDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(ConfigurationStoreOptions));
            if (configStoreDescriptor != null) services.Remove(configStoreDescriptor);

            var opStoreDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(OperationalStoreOptions));
            if (opStoreDescriptor != null) services.Remove(opStoreDescriptor);

            services.AddSingleton(new ConfigurationStoreOptions
            {
                ConfigureDbContext = b => b.UseInMemoryDatabase($"TestConfigDb-{_dbName}")
            });

            services.AddSingleton(new OperationalStoreOptions
            {
                ConfigureDbContext = b => b.UseInMemoryDatabase($"TestGrantDb-{_dbName}")
            });

            // Register InMemory DbContexts
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

            // Replace authentication with a test scheme
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
                options.DefaultScheme = "TestScheme";
            });

            // Register a no-op IServerSideSessionStore for GrantsSessions page
            services.AddSingleton<IServerSideSessionStore, InMemoryServerSideSessionStore>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure databases are created after the host is built
        using var scope = host.Services.CreateScope();

        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        appDb.Database.EnsureCreated();

        return host;
    }
}

/// <summary>
/// Simple in-memory IServerSideSessionStore for integration tests.
/// </summary>
internal class InMemoryServerSideSessionStore : IServerSideSessionStore
{
    private readonly List<ServerSideSession> _sessions = new();

    public Task CreateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s => s.Key == key);
        return Task.CompletedTask;
    }

    public Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s =>
            (filter.SubjectId == null || s.SubjectId == filter.SubjectId) &&
            (filter.SessionId == null || s.SessionId == filter.SessionId));
        return Task.CompletedTask;
    }

    public Task<ServerSideSession?> GetSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.FirstOrDefault(s => s.Key == key));
    }

    public Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        var results = _sessions.Where(s =>
            (filter.SubjectId == null || s.SubjectId == filter.SubjectId) &&
            (filter.SessionId == null || s.SessionId == filter.SessionId))
            .ToList();
        return Task.FromResult<IReadOnlyCollection<ServerSideSession>>(results);
    }

    public Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        DeleteSessionAsync(session.Key, cancellationToken);
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<QueryResult<ServerSideSession>> QuerySessionsAsync(SessionQuery? filter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new QueryResult<ServerSideSession>
        {
            Results = _sessions,
            HasPrevResults = false,
            HasNextResults = false,
            TotalCount = _sessions.Count,
            TotalPages = 1,
            CurrentPage = 1
        });
    }

    public Task<IReadOnlyCollection<ServerSideSession>> GetAndRemoveExpiredSessionsAsync(int count, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<ServerSideSession>>(Array.Empty<ServerSideSession>());
    }
}
