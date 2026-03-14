using IdentityServerAspNetIdentity.Pages.Admin.Roles;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using FluentAssertions;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Roles;

public class EditModelTests
{
    private readonly Mock<IRolesAdminService> _mockRolesAdminService;
    private readonly EditModel _sut;

    public EditModelTests()
    {
        _mockRolesAdminService = new Mock<IRolesAdminService>();
        
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        _sut = new EditModel(_mockRolesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = tempData
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        _sut.RoleId = "non-existent";
        _mockRolesAdminService
            .Setup(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleEditPageDataDto?)null);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateData_WhenRoleExists()
    {
        // Arrange
        _sut.RoleId = "role-1";
        var pageData = new RoleEditPageDataDto
        {
            RoleName = "Admin",
            UsersInRole = new List<RoleUserDto> { new() { UserName = "user1" } },
            AvailableUsers = new List<RoleUserDto> { new() { UserName = "user2" } }
        };

        _mockRolesAdminService
            .Setup(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.RoleName.Should().Be("Admin");
        _sut.UsersInRole.Should().HaveCount(1);
        _sut.AvailableUsers.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithError_WhenNoUserSelected()
    {
        // Arrange
        _sut.SelectedUserId = null;
        
        // Mocking ReloadPageData dependencies
        _mockRolesAdminService
            .Setup(x => x.GetRoleForEditAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleEditPageDataDto());

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Should().ContainKey(nameof(_sut.SelectedUserId));
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnNotFound_WhenServiceReportsNotFound()
    {
        // Arrange
        _sut.RoleId = "role-1";
        _sut.SelectedUserId = "user-1";
        
        _mockRolesAdminService
            .Setup(x => x.AddUserToRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult { Status = AddUserToRoleStatus.RoleNotFound });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldRedirectWithSuccess_WhenUserAddedSuccessfully()
    {
        // Arrange
        _sut.RoleId = "role-1";
        _sut.SelectedUserId = "user-1";
        
        _mockRolesAdminService
            .Setup(x => x.AddUserToRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult 
            { 
                Status = AddUserToRoleStatus.Success,
                UserName = "Alice",
                RoleName = "Admin"
            });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.RouteValues.Should().ContainKey("roleId").WhoseValue.Should().Be("role-1");
        _sut.TempData["Success"].Should().Be("User 'Alice' added to role 'Admin'");
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithErrors_WhenServiceReportsFailure()
    {
        // Arrange
        _sut.RoleId = "role-1";
        _sut.SelectedUserId = "user-1";
        var errors = new List<string> { "Error 1", "Error 2" };

        _mockRolesAdminService
            .Setup(x => x.AddUserToRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Failed,
                Errors = errors
            });

        // Mocking ReloadPageData dependencies
        _mockRolesAdminService
            .Setup(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleEditPageDataDto { RoleName = "Admin" });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(2);
        _sut.RoleName.Should().Be("Admin"); // Proves ReloadPageData was called
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnPageWithError_WhenNoUserSelected()
    {
        // Arrange
        _sut.SelectedUserId = null;
        
        _mockRolesAdminService
            .Setup(x => x.GetRoleForEditAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleEditPageDataDto());

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Should().ContainKey(nameof(_sut.SelectedUserId));
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldRedirectWithSuccess_WhenUserRemovedSuccessfully()
    {
        // Arrange
        _sut.RoleId = "role-1";
        _sut.SelectedUserId = "user-1";
        
        _mockRolesAdminService
            .Setup(x => x.RemoveUserFromRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult 
            { 
                Status = RemoveUserFromRoleStatus.Success,
                UserName = "Alice",
                RoleName = "Admin"
            });

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        _sut.TempData["Success"].Should().Be("User 'Alice' removed from role 'Admin'");
    }
}
