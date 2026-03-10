using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using FluentAssertions;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class IdentityResourcesAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ConfigurationDbContext _configDbContext = default!;
    private IIdentityResourcesAdminService _sut = default!;

    public IdentityResourcesAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _configDbContext = _scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        _sut = _scope.ServiceProvider.GetRequiredService<IIdentityResourcesAdminService>();

        _configDbContext.IdentityResources.RemoveRange(_configDbContext.IdentityResources);
        return _configDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_ShouldReturnSortedDtos_WhenResourcesExist()
    {
        // Arrange
        _configDbContext.IdentityResources.AddRange(
            new IdentityResource { Name = "Z_Resource", DisplayName = "Z" },
            new IdentityResource { Name = "A_Resource", DisplayName = "A" }
        );
        await _configDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetIdentityResourcesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("A_Resource");
        result[1].Name.Should().Be("Z_Resource");
    }
}
