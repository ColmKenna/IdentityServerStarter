using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class IdentityResourcesAdminServiceTests : IDisposable
{
    private readonly SqliteConnection _configurationConnection;
    private readonly DbContextOptions<ConfigurationDbContext> _configurationOptions;

    public IdentityResourcesAdminServiceTests()
    {
        _configurationConnection = new SqliteConnection("Data Source=:memory:");
        _configurationConnection.Open();

        var storeOptions = new ConfigurationStoreOptions();
        _configurationOptions = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite(_configurationConnection)
            .UseApplicationServiceProvider(
                new ServiceCollection()
                    .AddSingleton(storeOptions)
                    .BuildServiceProvider())
            .Options;

        using var configurationDbContext = CreateConfigurationContext();
        configurationDbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _configurationConnection.Dispose();
    }

    private ConfigurationDbContext CreateConfigurationContext()
    {
        return new ConfigurationDbContext(_configurationOptions);
    }

    private IdentityResourcesAdminService CreateService(ConfigurationDbContext configCtx)
    {
        return new IdentityResourcesAdminService(configCtx);
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_NoResources_ReturnsEmptyList()
    {
        using var configurationDbContext = CreateConfigurationContext();
        var service = CreateService(configurationDbContext);

        var result = await service.GetIdentityResourcesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_ResourcesExist_ReturnsAllResources()
    {
        using (var seedContext = CreateConfigurationContext())
        {
            seedContext.IdentityResources.AddRange(
                new IdentityResource { Name = "openid", DisplayName = "OpenID", Description = "OpenID Connect", Enabled = true },
                new IdentityResource { Name = "profile", DisplayName = "Profile", Description = "User profile", Enabled = false });
            await seedContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        var service = CreateService(configurationDbContext);

        var result = await service.GetIdentityResourcesAsync();

        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().Contain(new[] { "openid", "profile" });
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_ResourcesExist_ReturnedInAscendingOrderByName()
    {
        using (var seedContext = CreateConfigurationContext())
        {
            seedContext.IdentityResources.AddRange(
                new IdentityResource { Name = "profile", Enabled = true },
                new IdentityResource { Name = "address", Enabled = true },
                new IdentityResource { Name = "openid", Enabled = true });
            await seedContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        var service = CreateService(configurationDbContext);

        var result = await service.GetIdentityResourcesAsync();

        result.Select(r => r.Name).Should().Equal("address", "openid", "profile");
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_MapsAllFields()
    {
        using (var seedContext = CreateConfigurationContext())
        {
            seedContext.IdentityResources.Add(
                new IdentityResource
                {
                    Name = "openid",
                    DisplayName = "OpenID Connect",
                    Description = "The OpenID Connect scope",
                    Enabled = true
                });
            await seedContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        var service = CreateService(configurationDbContext);

        var result = await service.GetIdentityResourcesAsync();

        var item = result.Should().ContainSingle().Subject;
        item.Name.Should().Be("openid");
        item.DisplayName.Should().Be("OpenID Connect");
        item.Description.Should().Be("The OpenID Connect scope");
        item.Enabled.Should().BeTrue();
        item.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_NullDisplayNameAndDescription_ReturnsEmptyStrings()
    {
        using (var seedContext = CreateConfigurationContext())
        {
            seedContext.IdentityResources.Add(
                new IdentityResource
                {
                    Name = "openid",
                    DisplayName = null,
                    Description = null,
                    Enabled = true
                });
            await seedContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        var service = CreateService(configurationDbContext);

        var result = await service.GetIdentityResourcesAsync();

        var item = result.Should().ContainSingle().Subject;
        item.DisplayName.Should().Be(string.Empty);
        item.Description.Should().Be(string.Empty);
    }
}
