using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClaimsEditModelTests
{
    private readonly Mock<IClaimsAdminService> _mockClaimsAdminService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Claims.EditModel _pageModel;

    public ClaimsEditModelTests()
    {
        _mockClaimsAdminService = new Mock<IClaimsAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.EditModel(_mockClaimsAdminService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private static ClaimEditPageDataDto CreatePageData(
        IReadOnlyList<ClaimUserAssignmentItemDto>? usersInClaim = null,
        IReadOnlyList<AvailableClaimUserItemDto>? availableUsers = null,
        string? newClaimValue = null)
    {
        return new ClaimEditPageDataDto
        {
            UsersInClaim = usersInClaim ?? new List<ClaimUserAssignmentItemDto>
            {
                new()
                {
                    UserId = "u1",
                    UserName = "alice",
                    Email = "alice@test.com",
                    ClaimValue = "engineering",
                    IsLastUserAssignment = false
                }
            },
            AvailableUsers = availableUsers ?? new List<AvailableClaimUserItemDto>
            {
                new()
                {
                    UserId = "u2",
                    UserName = "bob",
                    Email = "bob@test.com"
                }
            },
            NewClaimValue = newClaimValue
        };
    }

    [Fact]
    public async Task OnGetAsync_MissingClaimType_ReturnsBadRequest()
    {
        _pageModel.ClaimType = string.Empty;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<BadRequestResult>();
        _mockClaimsAdminService.Verify(
            service => service.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_UnknownClaimType_ReturnsNotFound()
    {
        _pageModel.ClaimType = "unknown";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("unknown", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimEditPageDataDto?)null);

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ValidClaimType_MapsPageData_AndReturnsPage()
    {
        _pageModel.ClaimType = "department";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(
                usersInClaim: new List<ClaimUserAssignmentItemDto>
                {
                    new()
                    {
                        UserId = "u1",
                        UserName = "alice",
                        Email = "alice@test.com",
                        ClaimValue = "engineering",
                        IsLastUserAssignment = true
                    }
                },
                availableUsers: new List<AvailableClaimUserItemDto>
                {
                    new()
                    {
                        UserId = "u2",
                        UserName = "bob",
                        Email = "bob@test.com"
                    }
                },
                newClaimValue: "true"));

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.UsersInClaim.Should().ContainSingle();
        _pageModel.UsersInClaim[0].UserName.Should().Be("alice");
        _pageModel.UsersInClaim[0].IsLastUserAssignment.Should().BeTrue();
        _pageModel.AvailableUsers.Should().ContainSingle();
        _pageModel.AvailableUsers[0].UserName.Should().Be("bob");
        _pageModel.NewClaimValue.Should().Be("true");
    }

    [Fact]
    public async Task OnPostAddUserAsync_MissingClaimType_ReturnsBadRequest()
    {
        _pageModel.ClaimType = " ";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = "engineering";

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<BadRequestResult>();
        _mockClaimsAdminService.Verify(
            service => service.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAddUserAsync_UnknownClaimType_ReturnsNotFound()
    {
        _pageModel.ClaimType = "unknown";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("unknown", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimEditPageDataDto?)null);

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_AddsModelError_SelectedUserId_ExactMessage()
    {
        _pageModel.ClaimType = "department";
        _pageModel.NewClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(nameof(_pageModel.SelectedUserId));
        _pageModel.ModelState[nameof(_pageModel.SelectedUserId)]!.Errors.Single().ErrorMessage
            .Should().Be("Please select a user");
        _mockClaimsAdminService.Verify(
            service => service.AddUserToClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAddUserAsync_MissingClaimValue_AddsModelError_NewClaimValue_ExactMessage()
    {
        _pageModel.ClaimType = "department";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = string.Empty;
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(nameof(_pageModel.NewClaimValue));
        _pageModel.ModelState[nameof(_pageModel.NewClaimValue)]!.Errors.Single().ErrorMessage
            .Should().Be("Claim value is required");
        _mockClaimsAdminService.Verify(
            service => service.AddUserToClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAddUserAsync_UserNotFound_ReturnsNotFound()
    {
        _pageModel.ClaimType = "department";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.AddUserToClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.UserNotFound
            });

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_AlreadyAssigned_AddsModelError_ModelOnly_ExactMessage()
    {
        _pageModel.ClaimType = "department";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.AddUserToClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.AlreadyAssigned
            });

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(string.Empty);
        _pageModel.ModelState[string.Empty]!.Errors.Single().ErrorMessage
            .Should().Be("Selected user already has this claim type");
    }

    [Fact]
    public async Task OnPostAddUserAsync_IdentityFailure_AddsModelOnlyErrors_ExactDescriptions()
    {
        _pageModel.ClaimType = "department";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.AddUserToClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.IdentityFailure,
                Errors = new List<string> { "error-1", "error-2" }
            });

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(string.Empty);
        _pageModel.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage)
            .Should().Equal("error-1", "error-2");
    }

    [Fact]
    public async Task OnPostAddUserAsync_Success_RedirectsToEdit_WithExactSuccessTempData()
    {
        _pageModel.ClaimType = "department";
        _pageModel.SelectedUserId = "u1";
        _pageModel.NewClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.AddUserToClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddClaimAssignmentResult
            {
                Status = AddClaimAssignmentStatus.Success,
                UserName = "alice"
            });

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/Claims/Edit");
        redirect.RouteValues.Should().ContainKey("claimType");
        redirect.RouteValues!["claimType"].Should().Be("department");
        _pageModel.TempData["Success"].Should().Be("Claim 'department' assigned to user 'alice'.");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_MissingClaimType_ReturnsBadRequest()
    {
        _pageModel.ClaimType = " ";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<BadRequestResult>();
        _mockClaimsAdminService.Verify(
            service => service.GetForEditAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_UnknownClaimType_ReturnsNotFound()
    {
        _pageModel.ClaimType = "unknown";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("unknown", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClaimEditPageDataDto?)null);

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_MissingAssignmentDetails_AddsModelError_ModelOnly_ExactMessage()
    {
        _pageModel.ClaimType = "department";
        _pageModel.RemoveUserId = string.Empty;
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(string.Empty);
        _pageModel.ModelState[string.Empty]!.Errors.Single().ErrorMessage
            .Should().Be("Claim assignment details are required");
        _mockClaimsAdminService.Verify(
            service => service.RemoveUserFromClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_UserNotFound_ReturnsNotFound()
    {
        _pageModel.ClaimType = "department";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.RemoveUserFromClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.UserNotFound
            });

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_AssignmentMissing_AddsModelError_ModelOnly_ExactMessage()
    {
        _pageModel.ClaimType = "department";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.RemoveUserFromClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.AssignmentNotFound
            });

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(string.Empty);
        _pageModel.ModelState[string.Empty]!.Errors.Single().ErrorMessage
            .Should().Be("The selected claim assignment does not exist");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_IdentityFailure_AddsModelOnlyErrors_ExactDescriptions()
    {
        _pageModel.ClaimType = "department";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.RemoveUserFromClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.IdentityFailure,
                Errors = new List<string> { "error-1", "error-2" }
            });

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey(string.Empty);
        _pageModel.ModelState[string.Empty]!.Errors.Select(error => error.ErrorMessage)
            .Should().Equal("error-1", "error-2");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_LastAssignmentRemoved_RedirectsIndex_WithExactWarningTempData()
    {
        _pageModel.ClaimType = "department";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.RemoveUserFromClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.Success,
                UserName = "alice",
                HasRemainingAssignments = false
            });

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/Claims/Index");
        _pageModel.TempData["Warning"].Should().Be(
            "Claim type 'department' no longer has any assigned users and was removed from the system. Assign it to a user with a different value to keep this claim.");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_RemainingAssignments_RedirectsEdit_WithExactSuccessTempData()
    {
        _pageModel.ClaimType = "department";
        _pageModel.RemoveUserId = "u1";
        _pageModel.RemoveClaimValue = "engineering";
        _mockClaimsAdminService
            .Setup(service => service.GetForEditAsync("department", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockClaimsAdminService
            .Setup(service => service.RemoveUserFromClaimAsync("department", "u1", "engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveClaimAssignmentResult
            {
                Status = RemoveClaimAssignmentStatus.Success,
                UserName = "alice",
                HasRemainingAssignments = true
            });

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/Claims/Edit");
        redirect.RouteValues.Should().ContainKey("claimType");
        redirect.RouteValues!["claimType"].Should().Be("department");
        _pageModel.TempData["Success"].Should().Be("Claim 'department' removed from user 'alice'.");
    }
}
