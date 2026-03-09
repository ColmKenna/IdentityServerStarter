using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System.Security.Claims;
using Xunit;
using MockQueryable;

namespace IdentityServerServices.UnitTests;

public class UserEditorTests : IDisposable
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IPersistedGrantStore> _grantStoreMock;
    private readonly Mock<IServerSideSessionStore> _sessionStoreMock;
    private readonly ApplicationDbContext _dbContext;

    public UserEditorTests()
    {
        _userManagerMock = MockUserManager<ApplicationUser>();
        _roleManagerMock = MockRoleManager<IdentityRole>();
        _grantStoreMock = new Mock<IPersistedGrantStore>();
        _sessionStoreMock = new Mock<IServerSideSessionStore>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private UserEditor CreateSut()
    {
        return new UserEditor(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _grantStoreMock.Object,
            _dbContext,
            _sessionStoreMock.Object);
    }

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        return new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
    {
        var store = new Mock<IRoleStore<TRole>>();
        return new Mock<RoleManager<TRole>>(store.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnMappedDtos_WhenUsersExist()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = "1", UserName = "Alice", Email = "alice@test.com", EmailConfirmed = true, LockoutEnd = DateTimeOffset.UtcNow, TwoFactorEnabled = false },
            new ApplicationUser { Id = "2", UserName = "Bob", Email = "bob@test.com", EmailConfirmed = false, LockoutEnd = null, TwoFactorEnabled = true }
        };

        var mockUsersQueryable = users.BuildMock();
        _userManagerMock.Setup(m => m.Users).Returns(mockUsersQueryable);

        var sut = CreateSut();

        // Act
        var result = await sut.GetUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].UserName.Should().Be("Alice");
        result[1].UserName.Should().Be("Bob");
        result[1].TwoFactorEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("missing-id")]
    public async Task GetUserEditPageDataAsync_ShouldReturnNull_WhenUserIdIsNullOrUserNotFound(string? userId)
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new UserEditPageDataRequest { UserId = userId! };
        var sut = CreateSut();

        // Act
        var result = await sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldConditionallyFetchData_BasedOnRequestFlags()
    {
        // Arrange
        var user = new ApplicationUser { Id = "123", UserName = "Test" };
        _userManagerMock.Setup(m => m.FindByIdAsync("123")).ReturnsAsync(user);

        var request = new UserEditPageDataRequest
        {
            UserId = "123",
            IncludeUserTabData = false,
            IncludeClaims = false,
            IncludeRoles = false,
            IncludeGrants = false,
            IncludeSessions = false
        };

        var sut = CreateSut();

        // Act
        var result = await sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Claims.Should().BeEmpty();
        result.Roles.Should().BeEmpty();
        result.Grants.Should().BeEmpty();
        result.Sessions.Should().BeEmpty();

        _userManagerMock.Verify(m => m.GetClaimsAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _grantStoreMock.Verify(m => m.GetAllAsync(It.IsAny<PersistedGrantFilter>()), Times.Never);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldComputeAccountStatusAccurately()
    {
        // Arrange
        var activeUser = new ApplicationUser { Id = "1", LockoutEnd = DateTimeOffset.UtcNow.AddDays(-1) };
        var disabledUser = new ApplicationUser { Id = "2", LockoutEnd = DateTimeOffset.MaxValue };
        var lockedOutUser = new ApplicationUser { Id = "3", LockoutEnd = DateTimeOffset.UtcNow.AddDays(1) };
        
        _userManagerMock.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(activeUser);
        _userManagerMock.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(disabledUser);
        _userManagerMock.Setup(m => m.FindByIdAsync("3")).ReturnsAsync(lockedOutUser);

        _userManagerMock.Setup(m => m.GetLoginsAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);
        _userManagerMock.Setup(m => m.GetValidTwoFactorProvidersAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);

        var sut = CreateSut();
        var request = (string id) => new UserEditPageDataRequest { UserId = id, IncludeUserTabData = true };

        // Act
        var activeResult = await sut.GetUserEditPageDataAsync(request("1"));
        var disabledResult = await sut.GetUserEditPageDataAsync(request("2"));
        var lockedResult = await sut.GetUserEditPageDataAsync(request("3"));

        // Assert
        activeResult!.AccountStatus.Should().Be("Active");
        disabledResult!.AccountStatus.Should().Be("Disabled");
        lockedResult!.AccountStatus.Should().StartWith("Locked Out (until");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnEarlyWithFailure_WhenUpdateAsyncFails()
    {
        // Arrange
        var user = new ApplicationUser { Id = "123" };
        _userManagerMock.Setup(m => m.FindByIdAsync("123")).ReturnsAsync(user);

        var expectedError = IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Update failed" });
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(expectedError);

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            Profile = new UserProfileEditViewModel { Username = "NewName" },
            LockoutEnabled = true // This should not execute due to UpdateAsync failure
        };

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateUserFromEditPostAsync(request);

        // Assert
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.First().Description.Should().Be("Update failed");

        _userManagerMock.Verify(m => m.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldApplyAllUpdates_AndReturnSuccess()
    {
        // Arrange
        var user = new ApplicationUser { Id = "123" };
        _userManagerMock.Setup(m => m.FindByIdAsync("123")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetTwoFactorEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
        
        _userManagerMock.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddPasswordAsync(user, "NewStrongPassword1!")).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            Profile = new UserProfileEditViewModel { Username = "NewName" },
            LockoutEnabled = true,
            TwoFactorEnabled = true,
            NewPassword = "NewStrongPassword1!"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateUserFromEditPostAsync(request);

        // Assert
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();

        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
        _userManagerMock.Verify(m => m.SetLockoutEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(m => m.SetTwoFactorEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(m => m.RemovePasswordAsync(user), Times.Once);
        _userManagerMock.Verify(m => m.AddPasswordAsync(user, "NewStrongPassword1!"), Times.Once);
    }
}
