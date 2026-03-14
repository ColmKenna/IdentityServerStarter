using IdentityServerAspNetIdentity.Pages.Admin.Users;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin;

public class UsersIndexModelTests
{
    private readonly Mock<IUserEditor> _mockUserEditor;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Users.Index _sut;

    public UsersIndexModelTests()
    {
        _mockUserEditor = new Mock<IUserEditor>();
        _sut = new IdentityServerAspNetIdentity.Pages.Admin.Users.Index(_mockUserEditor.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateUsersFromService()
    {
        // Arrange
        var expectedUsers = new List<UserListItemDto>
        {
            new() { Id = "1", UserName = "alice" },
            new() { Id = "2", UserName = "bob" }
        };
        _mockUserEditor.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Users.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsEmpty()
    {
        // Arrange
        _mockUserEditor.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPassCancellationToken_ToService()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _sut.PageContext.HttpContext.RequestAborted = cts.Token;
        
        _mockUserEditor.Setup(x => x.GetUsersAsync(cts.Token))
            .ReturnsAsync(new List<UserListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _mockUserEditor.Verify(x => x.GetUsersAsync(cts.Token), Times.Once);
    }
}
