using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class UsersIndexModelTests
{
    private readonly Mock<IUserEditor> _mockService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Users.Index _pageModel;

    public UsersIndexModelTests()
    {
        _mockService = new Mock<IUserEditor>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Users.Index(_mockService.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesUsersCollection()
    {
        _mockService
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsUsers_PopulatesUsers()
    {
        _mockService
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserListItemDto>
            {
                new()
                {
                    Id = "u1",
                    UserName = "alice",
                    Email = "alice@test.com",
                    EmailConfirmed = true,
                    TwoFactorEnabled = false
                },
                new()
                {
                    Id = "u2",
                    UserName = "bob",
                    Email = "bob@test.com",
                    EmailConfirmed = false,
                    TwoFactorEnabled = true
                }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().HaveCount(2);
        _pageModel.Users.Select(u => u.UserName).Should().Contain(new[] { "alice", "bob" });
    }

    [Fact]
    public async Task OnGetAsync_NoUsers_ReturnsEmptyList()
    {
        _mockService
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_CallsServiceOnce()
    {
        _mockService
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserListItemDto>());

        await _pageModel.OnGetAsync();

        _mockService.Verify(
            s => s.GetUsersAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_UserWithLockout_PreservesLockoutEnd()
    {
        var lockoutEnd = DateTimeOffset.MaxValue;
        _mockService
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserListItemDto>
            {
                new() { Id = "u1", UserName = "alice", LockoutEnd = lockoutEnd }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().ContainSingle().Which.LockoutEnd.Should().Be(lockoutEnd);
    }
}
