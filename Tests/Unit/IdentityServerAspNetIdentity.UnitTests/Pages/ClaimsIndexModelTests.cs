using IdentityServer.EF.DataAccess.DataMigrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClaimsIndexModelTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task OnGetAsync_InitializesClaimsCollection()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.Claims.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_UserClaimsExist_PopulatesDistinctClaimTypes()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "sales" },
            new IdentityUserClaim<string> { UserId = "u3", ClaimType = "location", ClaimValue = "dublin" });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.Claims.Select(c => c.ClaimType).Should().BeEquivalentTo(new[] { "department", "location" });
    }

    [Fact]
    public async Task OnGetAsync_ClaimTypesAreOrderedAlphabetically()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "zeta", ClaimValue = "1" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "alpha", ClaimValue = "2" },
            new IdentityUserClaim<string> { UserId = "u3", ClaimType = "middle", ClaimValue = "3" });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.Claims.Select(c => c.ClaimType).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OnGetAsync_NoClaimsExist_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.Claims.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_NullOrEmptyClaimTypes_AreExcluded()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = string.Empty, ClaimValue = "v1" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "v2" });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.Claims.Select(c => c.ClaimType).Should().BeEquivalentTo(new[] { "department" });
    }
}
