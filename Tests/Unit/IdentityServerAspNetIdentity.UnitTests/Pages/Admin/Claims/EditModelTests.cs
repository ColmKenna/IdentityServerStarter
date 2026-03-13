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

        var httpContext = new DefaultHttpContext();
        _sut = new EditModel(_mockClaimsAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    // --------- OnGetAsync ---------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task OnGetAsync_ShouldReturnBadRequest_WhenClaimTypeIsWhitespace(string? claimType)
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
    public async Task OnGetAsync_ShouldPopulatePageData_WhenServiceReturnsData()
    {
        // Arrange
        _sut.ClaimType = "Role";

        var pageData = new ClaimEditPageDataDto
        {
            UsersInClaim = [new ClaimUserAssignmentItemDto { UserId = "1", UserName = "alice", Email = "a@test.com", ClaimValue = "admin" }],
            AvailableUsers = [new AvailableClaimUserItemDto { UserId = "2", UserName = "bob", Email = "b@test.com" }],
            NewClaimValue = "admin"
        };

        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync("Role", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.UsersInClaim.Should().BeEquivalentTo(pageData.UsersInClaim);
        _sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
        _sut.NewClaimValue.Should().Be("admin");
    }

    // --------- OnPostAddUserAsync ---------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task OnPostAddUserAsync_ShouldReturnBadRequest_WhenClaimTypeIsWhitespace(string? claimType)
    {
        // Arrange
        _sut.ClaimType = claimType;

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimEditPageDataDto?)null);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithModelError_WhenSelectedUserIdIsEmpty()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = string.Empty;
        _sut.NewClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto
        {
            UsersInClaim = [],
            AvailableUsers = [new AvailableClaimUserItemDto { UserId = "1", UserName = "TestUser" }]
        };

        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync("Role", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[nameof(_sut.SelectedUserId)]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Please select a user");
        _sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithModelError_WhenNewClaimValueIsWhitespace()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = "user-123";
        _sut.NewClaimValue = " ";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync("Role", " ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[nameof(_sut.NewClaimValue)]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Claim value is required");
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnNotFound_WhenStatusIsUserNotFound()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = "user-123";
        _sut.NewClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.AddUserToClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult { Status = AddClaimAssignmentStatus.UserNotFound });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithModelError_WhenStatusIsAlreadyAssigned()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = "user-123";
        _sut.NewClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.AddUserToClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.AlreadyAssigned,
                UserName = "alice"
            });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[string.Empty]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Selected user already has this claim type");
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldReturnPageWithAllErrors_WhenStatusIsIdentityFailure()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = "user-123";
        _sut.NewClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.AddUserToClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.IdentityFailure,
                Errors = ["Identity error 1", "Identity error 2"]
            });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        var errors = _sut.ModelState[string.Empty]!.Errors;
        errors.Should().HaveCount(2);
        errors.Select(e => e.ErrorMessage).Should().Contain("Identity error 1");
        errors.Select(e => e.ErrorMessage).Should().Contain("Identity error 2");
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldSetTempDataAndRedirect_OnSuccess()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.SelectedUserId = "user-123";
        _sut.NewClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.AddUserToClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.Success,
                UserName = "alice"
            });

        // Act
        var result = await _sut.OnPostAddUserAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Claims/Edit");
        redirect.RouteValues!["claimType"].Should().Be("Role");
        _sut.TempData["Success"].Should().Be("Claim 'Role' assigned to user 'alice'.");
    }

    // --------- OnPostRemoveUserAsync ---------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task OnPostRemoveUserAsync_ShouldReturnBadRequest_WhenClaimTypeIsWhitespace(string? claimType)
    {
        // Arrange
        _sut.ClaimType = claimType;

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimEditPageDataDto?)null);

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnPageWithModelError_WhenRemoveUserIdIsWhitespace()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = " ";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync("Role", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[string.Empty]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Claim assignment details are required");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnPageWithModelError_WhenRemoveClaimValueIsNull()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = null;

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync("Role", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[string.Empty]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Claim assignment details are required");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnNotFound_WhenStatusIsUserNotFound()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.RemoveUserFromClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult { Status = RemoveClaimAssignmentStatus.UserNotFound });

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnPageWithModelError_WhenStatusIsAssignmentNotFound()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.RemoveUserFromClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult { Status = RemoveClaimAssignmentStatus.AssignmentNotFound });

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[string.Empty]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The selected claim assignment does not exist");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldReturnPageWithAllErrors_WhenStatusIsIdentityFailure()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.RemoveUserFromClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.IdentityFailure,
                Errors = ["Remove error 1"]
            });

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[string.Empty]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Remove error 1");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldRedirectToIndexWithWarning_WhenNoRemainingAssignments()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.RemoveUserFromClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.Success,
                HasRemainingAssignments = false,
                UserName = "alice"
            });

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Claims/Index");
        _sut.TempData["Warning"].Should().NotBeNull();
        _sut.TempData["Warning"]!.ToString().Should().Contain("Role");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldRedirectToEditWithSuccess_WhenRemainingAssignmentsExist()
    {
        // Arrange
        _sut.ClaimType = "Role";
        _sut.RemoveUserId = "user-123";
        _sut.RemoveClaimValue = "Admin";

        var pageData = new ClaimEditPageDataDto();
        _mockClaimsAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockClaimsAdminService
            .Setup(x => x.RemoveUserFromClaimAsync("Role", "user-123", "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.Success,
                HasRemainingAssignments = true,
                UserName = "alice"
            });

        // Act
        var result = await _sut.OnPostRemoveUserAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Claims/Edit");
        redirect.RouteValues!["claimType"].Should().Be("Role");
        _sut.TempData["Success"].Should().Be("Claim 'Role' removed from user 'alice'.");
    }
}
