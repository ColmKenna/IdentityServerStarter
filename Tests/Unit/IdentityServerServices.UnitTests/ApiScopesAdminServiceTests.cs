using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using IdentityServerServices.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class ApiScopesAdminServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ConfigurationDbContext> _options;

    public ApiScopesAdminServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var storeOptions = new ConfigurationStoreOptions();
        _options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite(_connection)
            .UseApplicationServiceProvider(
                new ServiceCollection()
                    .AddSingleton(storeOptions)
                    .BuildServiceProvider())
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private ConfigurationDbContext CreateContext()
    {
        return new ConfigurationDbContext(_options);
    }

    private ApiScopesAdminService CreateService(ConfigurationDbContext context)
    {
        return new ApiScopesAdminService(context);
    }

    [Fact]
    public async Task GetApiScopesAsync_NoScopes_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetApiScopesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiScopesAsync_ApiScopesExist_ReturnsAllScopes()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.ApiScopes.AddRange(
                new ApiScope { Name = "orders.read", Enabled = true },
                new ApiScope { Name = "orders.write", Enabled = false });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetApiScopesAsync();

        result.Should().HaveCount(2);
        result.Select(scope => scope.Name).Should().Contain(new[] { "orders.read", "orders.write" });
    }

    [Fact]
    public async Task GetApiScopesAsync_OrdersByNameAscending()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.ApiScopes.AddRange(
                new ApiScope { Name = "zeta", Enabled = true },
                new ApiScope { Name = "alpha", Enabled = true },
                new ApiScope { Name = "middle", Enabled = true });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetApiScopesAsync();

        result.Select(scope => scope.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetApiScopesAsync_NullDisplayNameOrDescription_MapsToEmptyStrings()
    {
        using (var seedContext = CreateContext())
        {
            seedContext.ApiScopes.Add(new ApiScope
            {
                Name = "api1",
                DisplayName = null,
                Description = null,
                Enabled = true
            });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetApiScopesAsync();

        result.Should().ContainSingle();
        result[0].DisplayName.Should().BeEmpty();
        result[0].Description.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiScopesAsync_MapsIdNameDisplayNameDescriptionEnabled()
    {
        int scopeId;
        using (var seedContext = CreateContext())
        {
            var entity = new ApiScope
            {
                Name = "orders.read",
                DisplayName = "Orders Read",
                Description = "Read orders",
                Enabled = false
            };
            seedContext.ApiScopes.Add(entity);
            await seedContext.SaveChangesAsync();
            scopeId = entity.Id;
        }

        using var context = CreateContext();
        var service = CreateService(context);

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
