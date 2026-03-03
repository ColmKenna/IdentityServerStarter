using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerServices.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class ApiScopesAdminServiceTests : IDisposable
{
    private readonly SqliteConnection _configurationConnection;
    private readonly SqliteConnection _applicationConnection;
    private readonly DbContextOptions<ConfigurationDbContext> _configurationOptions;
    private readonly DbContextOptions<ApplicationDbContext> _applicationOptions;

    public ApiScopesAdminServiceTests()
    {
        _configurationConnection = new SqliteConnection("Data Source=:memory:");
        _configurationConnection.Open();

        _applicationConnection = new SqliteConnection("Data Source=:memory:");
        _applicationConnection.Open();

        var storeOptions = new ConfigurationStoreOptions();
        _configurationOptions = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite(_configurationConnection)
            .UseApplicationServiceProvider(
                new ServiceCollection()
                    .AddSingleton(storeOptions)
                    .BuildServiceProvider())
            .Options;

        _applicationOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_applicationConnection)
            .Options;

        using var configurationDbContext = CreateConfigurationContext();
        configurationDbContext.Database.EnsureCreated();

        using var applicationDbContext = CreateApplicationContext();
        applicationDbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _configurationConnection.Dispose();
        _applicationConnection.Dispose();
    }

    private ConfigurationDbContext CreateConfigurationContext()
    {
        return new ConfigurationDbContext(_configurationOptions);
    }

    private ApplicationDbContext CreateApplicationContext()
    {
        return new ApplicationDbContext(_applicationOptions);
    }

    private ApiScopesAdminService CreateService(
        ConfigurationDbContext configurationDbContext,
        ApplicationDbContext applicationDbContext)
    {
        return new ApiScopesAdminService(configurationDbContext, applicationDbContext);
    }

    [Fact]
    public async Task GetApiScopesAsync_NoScopes_ReturnsEmptyList()
    {
        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetApiScopesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiScopesAsync_ApiScopesExist_ReturnsAllScopes()
    {
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            seedConfigurationDbContext.ApiScopes.AddRange(
                new ApiScope { Name = "orders.read", Enabled = true },
                new ApiScope { Name = "orders.write", Enabled = false });
            await seedConfigurationDbContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetApiScopesAsync();

        result.Should().HaveCount(2);
        result.Select(scope => scope.Name).Should().Contain(new[] { "orders.read", "orders.write" });
    }

    [Fact]
    public async Task GetApiScopesAsync_OrdersByNameAscending()
    {
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            seedConfigurationDbContext.ApiScopes.AddRange(
                new ApiScope { Name = "zeta", Enabled = true },
                new ApiScope { Name = "alpha", Enabled = true },
                new ApiScope { Name = "middle", Enabled = true });
            await seedConfigurationDbContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetApiScopesAsync();

        result.Select(scope => scope.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetApiScopesAsync_NullDisplayNameOrDescription_MapsToEmptyStrings()
    {
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            seedConfigurationDbContext.ApiScopes.Add(new ApiScope
            {
                Name = "api1",
                DisplayName = null,
                Description = null,
                Enabled = true
            });
            await seedConfigurationDbContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetApiScopesAsync();

        result.Should().ContainSingle();
        result[0].DisplayName.Should().BeEmpty();
        result[0].Description.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiScopesAsync_MapsIdNameDisplayNameDescriptionEnabled()
    {
        int scopeId;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var entity = new ApiScope
            {
                Name = "orders.read",
                DisplayName = "Orders Read",
                Description = "Read orders",
                Enabled = false
            };
            seedConfigurationDbContext.ApiScopes.Add(entity);
            await seedConfigurationDbContext.SaveChangesAsync();
            scopeId = entity.Id;
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetApiScopesAsync();

        result.Should().ContainSingle();
        var scope = result[0];
        scope.Should().BeEquivalentTo(new ApiScopeListItemDto
        {
            Id = scopeId,
            Name = "orders.read",
            DisplayName = "Orders Read",
            Description = "Read orders",
            Enabled = false
        });
    }
}
