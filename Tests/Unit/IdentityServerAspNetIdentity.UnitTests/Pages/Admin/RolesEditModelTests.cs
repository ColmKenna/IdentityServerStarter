using FluentAssertions;
using IdentityServerAspNetIdentity.Pages.Admin.Roles;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin;

public class RolesEditModelTests
{
    private readonly Mock<IRolesAdminService> _mockRolesAdminService;
    private readonly EditModel _sut;

    public RolesEditModelTests()
    {
        _mockRolesAdminService = new Mock<IRolesAdminService>();
        _sut = new EditModel(_mockRolesAdminService.Object);
        SetupPageContext(_sut);
    }

    private static void SetupPageContext(PageModel pageModel)
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        pageModel.PageContext = new PageContext(actionContext) { ViewData = viewData };
        pageModel.TempData = tempData;
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        _sut.RoleId = "missing-role-id";
        _mockRolesAdminService.Setup(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleEditPageDataDto?)null);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulatePageDataAndReturnPage_WhenRoleExists()
    {
        // Arrange
        _sut.RoleId = "role-id";
        var pageData = new RoleEditPageDataDto
        {
            RoleName = "Admin",
            UsersInRole = new List<RoleUserDto> { new() { Id = "u1", UserName = "alice" } },
            AvailableUsers = new List<RoleUserDto> { new() { Id = "u2", UserName = "bob" } }
        };

        _mockRolesAdminService.Setup(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.RoleName.Should().Be("Admin");
        _sut.UsersInRole.Should().BeEquivalentTo(pageData.UsersInRole);
        _sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithModelStateError_WhenSelectedUserIdMissing()
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = string.Empty;

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Should().ContainKey(nameof(EditModel.SelectedUserId));
    }

    [Theory]
    [InlineData(AddUserToRoleStatus.RoleNotFound)]
    [InlineData(AddUserToRoleStatus.UserNotFound)]
    public async Task OnPostAddUserAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound(AddUserToRoleStatus notFoundStatus)
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = "user-id";
        
        var failureResult = new AddUserToRoleResult { Status = notFoundStatus };
        _mockRolesAdminService.Setup(x => x.AddUserToRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldAddModelStateErrorsAndReturnPage_WhenServiceFails()
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = "user-id";
        
        var failureResult = new AddUserToRoleResult 
        { 
            Status = AddUserToRoleStatus.Failed,
            Errors = new List<string> { "DB Error" }
        };
        _mockRolesAdminService.Setup(x => x.AddUserToRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Values.Should().ContainSingle(v => v.Errors.Count > 0)
            .Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "DB Error");
            
        // Assert ReloadPageData was hit
        _mockRolesAdminService.Verify(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldSetTempDataAndRedirectToPage_WhenSuccessful()
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = "user-id";
        
        var successResult = new AddUserToRoleResult 
        { 
            Status = AddUserToRoleStatus.Success,
            RoleName = "Admin",
            UserName = "Alice"
        };
        _mockRolesAdminService.Setup(x => x.AddUserToRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.RouteValues.Should().ContainKey("roleId").WhoseValue.Should().Be("role-id");
        
        _sut.TempData["Success"].Should().Be("User 'Alice' added to role 'Admin'");
    }

    // --- RemoveUser Tests ---

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnPageWithModelStateError_WhenSelectedUserIdMissing()
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = null;

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Should().ContainKey(nameof(EditModel.SelectedUserId));
    }

    [Theory]
    [InlineData(RemoveUserFromRoleStatus.RoleNotFound)]
    [InlineData(RemoveUserFromRoleStatus.UserNotFound)]
    public async Task OnPostRemoveUserAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound(RemoveUserFromRoleStatus notFoundStatus)
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = "user-id";
        
        var failureResult = new RemoveUserFromRoleResult { Status = notFoundStatus };
        _mockRolesAdminService.Setup(x => x.RemoveUserFromRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldAddModelStateErrorsAndReturnPage_WhenServiceFails()
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = "user-id";
        
        var failureResult = new RemoveUserFromRoleResult 
        { 
            Status = RemoveUserFromRoleStatus.Failed,
            Errors = new List<string> { "Removal failed" }
        };
        _mockRolesAdminService.Setup(x => x.RemoveUserFromRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Values.Should().ContainSingle(v => v.Errors.Count > 0)
            .Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Removal failed");
            
        _mockRolesAdminService.Verify(x => x.GetRoleForEditAsync(_sut.RoleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldSetTempDataAndRedirectToPage_WhenSuccessful()
    {
        // Arrange
        _sut.RoleId = "role-id";
        _sut.SelectedUserId = "user-id";
        
        var successResult = new RemoveUserFromRoleResult 
        { 
            Status = RemoveUserFromRoleStatus.Success,
            RoleName = "Admin",
            UserName = "Alice"
        };
        _mockRolesAdminService.Setup(x => x.RemoveUserFromRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.RouteValues.Should().ContainKey("roleId").WhoseValue.Should().Be("role-id");
        
        _sut.TempData["Success"].Should().Be("User 'Alice' removed from role 'Admin'");
    }
}
