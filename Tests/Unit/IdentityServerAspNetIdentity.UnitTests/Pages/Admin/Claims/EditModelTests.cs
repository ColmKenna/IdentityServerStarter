using FluentAssertions;
using IdentityServerAspNetIdentity.Pages.Admin.Claims;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Claims;

public class EditModelTests
{
    private readonly Mock<IClaimsAdminService> _mockClaimsAdminService;
    private readonly EditModel _sut;

    public EditModelTests()
    {
        _mockClaimsAdminService = new Mock<IClaimsAdminService>();

        _sut = new EditModel(_mockClaimsAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = new Mock<ITempDataDictionary>().Object
        };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task OnGetAsync_ShouldReturnBadRequest_WhenClaimTypeIsWhitespace(string claimType)
    {
        // Arrange
        _sut.ClaimType = claimType;

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimEditPageDataDto?)null);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithErrors_WhenSelectedUserIdIsMissing()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = string.Empty; // Missing user
        _sut.NewClaimValue = "Admin";
        
        var pageData = new ClaimEditPageDataDto
        {
            UsersInClaim = new List<ClaimUserAssignmentItemDto>(),
            AvailableUsers = new List<AvailableClaimUserItemDto> { new AvailableClaimUserItemDto { UserId = "1", UserName = "TestUser" } }
        };
        
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync("Role", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(1);
        _sut.ModelState.ContainsKey(nameof(_sut.SelectedUserId)).Should().BeTrue();
        
        // Verify page data was re-applied
        _sut.AvailableUsers.Should().HaveCount(1);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldRedirectToPage_OnSuccess()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = "user-123";
        _sut.NewClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var successResult = new AddClaimAssignmentResult 
        { 
            Status = AddClaimAssignmentStatus.Success, 
            UserName = "testuser" 
        };

        _mockClaimsAdminService
            .Setup(x => x.AddUserToClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/Claims/Edit");
        redirectResult.RouteValues.Should().ContainKey("claimType").WhoseValue.Should().Be("Role");
        
        // In the test setup, we just check that ITempDataDictionary is accessed (it's mocked, but we could spy on it. Here we trust the assignment doesn't throw)
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldRedirectToIndexWithWarning_WhenLastUserRemoved()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var removeResult = new RemoveClaimAssignmentResult 
        { 
            Status = RemoveClaimAssignmentStatus.Success, 
            HasRemainingAssignments = false, // Critical bit: Last user removed
            UserName = "testuser" 
        };

        _mockClaimsAdminService
            .Setup(x => x.RemoveUserFromClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(removeResult);

        var tempDataMock = new Mock<ITempDataDictionary>();
        _sut.TempData = tempDataMock.Object;

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/Claims/Index");
        
        // Verify TempData warning was set
        tempDataMock.VerifySet(t => t["Warning"] = It.IsAny<string>(), Times.Once);
    }
}
