using IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ApiScopesEditModelTests
{
    private readonly Mock<IApiScopesAdminService> _mockApiScopesAdminService;
    private readonly EditModel _pageModel;

    public ApiScopesEditModelTests()
    {
        _mockApiScopesAdminService = new Mock<IApiScopesAdminService>();
        _pageModel = new EditModel(_mockApiScopesAdminService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private static ApiScopeEditPageDataDto CreatePageData(
        string name = "orders.read",
        string? displayName = "Orders Read",
        string? description = "Read orders",
        bool enabled = true,
        IReadOnlyList<string>? appliedClaims = null,
        IReadOnlyList<string>? availableClaims = null)
    {
        return new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = name,
                DisplayName = displayName,
                Description = description,
                Enabled = enabled
            },
            AppliedUserClaims = appliedClaims ?? new List<string> { "department" },
            AvailableUserClaims = availableClaims ?? new List<string> { "location", "region" }
        };
    }

    [Fact]
    public async Task OnGetAsync_IdZero_ReturnsPageWithCreateDefaultsAndAvailableClaims()
    {
        _pageModel.Id = 0;
        _mockApiScopesAdminService
            .Setup(service => service.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: string.Empty, displayName: null, description: null, enabled: true,
                appliedClaims: Array.Empty<string>(),
                availableClaims: new[] { "department", "region" }));

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.Input.Name.Should().BeEmpty();
        _pageModel.Input.Enabled.Should().BeTrue();
        _pageModel.AppliedUserClaims.Should().BeEmpty();
        _pageModel.AvailableUserClaims.Should().Equal("department", "region");
    }

    [Fact]
    public async Task OnGetAsync_EditId_ReturnsPageAndMapsInputAndClaims()
    {
        _pageModel.Id = 10;
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(
                name: "orders.write",
                displayName: "Orders Write",
                description: "Write orders",
                enabled: false,
                appliedClaims: new[] { "department", "location" },
                availableClaims: new[] { "region" }));

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.Input.Name.Should().Be("orders.write");
        _pageModel.Input.DisplayName.Should().Be("Orders Write");
        _pageModel.Input.Description.Should().Be("Write orders");
        _pageModel.Input.Enabled.Should().BeFalse();
        _pageModel.AppliedUserClaims.Should().Equal("department", "location");
        _pageModel.AvailableUserClaims.Should().Equal("region");
    }

    [Fact]
    public async Task OnGetAsync_Missing_ReturnsNotFound()
    {
        _pageModel.Id = 999;
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_Create_ModelStateInvalid_ReturnsPage_WithoutCallingCreate()
    {
        _pageModel.Id = 0;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = string.Empty,
            Enabled = true
        };
        _pageModel.ModelState.AddModelError("Input.Name", "Name is required");

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _mockApiScopesAdminService.Verify(
            service => service.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_Create_Duplicate_AddsModelError_InputName_ExactMessage()
    {
        _pageModel.Id = 0;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Orders Read",
            Description = "Read orders",
            Enabled = true
        };
        _mockApiScopesAdminService
            .Setup(service => service.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateApiScopeResult
            {
                Status = CreateApiScopeStatus.DuplicateName
            });

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("Input.Name");
        _pageModel.ModelState["Input.Name"]!.Errors.Single().ErrorMessage
            .Should().Be("An API scope with this name already exists.");
    }

    [Fact]
    public async Task OnPostAsync_Create_Success_RedirectsToEdit_WithCreatedId_AndTempDataSuccessExact()
    {
        _pageModel.Id = 0;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.create",
            DisplayName = "Orders Create",
            Description = "Create orders",
            Enabled = true
        };
        _mockApiScopesAdminService
            .Setup(service => service.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateApiScopeResult
            {
                Status = CreateApiScopeStatus.Success,
                CreatedId = 25
            });

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(25);
        _pageModel.TempData["Success"].Should().Be("API scope created successfully");
    }

    [Fact]
    public async Task OnPostAsync_Edit_MissingOnLoad_ReturnsNotFound()
    {
        _pageModel.Id = 15;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.write",
            Enabled = true
        };
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
        _mockApiScopesAdminService.Verify(
            service => service.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_Edit_ModelStateInvalid_RepopulatesClaimsWithoutOverwritingInput()
    {
        _pageModel.Id = 16;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "user-entered-name",
            DisplayName = "User Entered Display Name",
            Description = "User Entered Description",
            Enabled = false
        };
        _pageModel.ModelState.AddModelError("Input.Name", "Name is required");
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(16, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(
                name: "server-value",
                displayName: "Server Display",
                description: "Server Description",
                enabled: true,
                appliedClaims: new[] { "department" },
                availableClaims: new[] { "location", "region" }));

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.Input.Name.Should().Be("user-entered-name");
        _pageModel.Input.DisplayName.Should().Be("User Entered Display Name");
        _pageModel.Input.Description.Should().Be("User Entered Description");
        _pageModel.Input.Enabled.Should().BeFalse();
        _pageModel.AppliedUserClaims.Should().Equal("department");
        _pageModel.AvailableUserClaims.Should().Equal("location", "region");
    }

    [Fact]
    public async Task OnPostAsync_Edit_Duplicate_AddsModelError_InputName_ExactMessage()
    {
        _pageModel.Id = 17;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Duplicate",
            Description = "Duplicate",
            Enabled = true
        };
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(17, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.UpdateAsync(17, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateApiScopeResult
            {
                Status = UpdateApiScopeStatus.DuplicateName
            });

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("Input.Name");
        _pageModel.ModelState["Input.Name"]!.Errors.Single().ErrorMessage
            .Should().Be("An API scope with this name already exists.");
    }

    [Fact]
    public async Task OnPostAsync_Edit_UpdateNotFound_ReturnsNotFound()
    {
        _pageModel.Id = 18;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            Enabled = true
        };
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(18, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.UpdateAsync(18, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateApiScopeResult
            {
                Status = UpdateApiScopeStatus.NotFound
            });

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_Edit_Success_RedirectsAndTempDataSuccessExact()
    {
        _pageModel.Id = 19;
        _pageModel.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            Enabled = true
        };
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(19, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.UpdateAsync(19, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateApiScopeResult
            {
                Status = UpdateApiScopeStatus.Success
            });

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(19);
        _pageModel.TempData["Success"].Should().Be("API scope updated successfully");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_MissingScope_ReturnsNotFound()
    {
        _pageModel.Id = 20;
        _pageModel.SelectedClaimType = "department";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await _pageModel.OnPostAddClaimAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddClaimAsync_NoSelection_AddsModelError_SelectedClaimType_ExactMessage()
    {
        _pageModel.Id = 21;
        _pageModel.SelectedClaimType = "   ";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var result = await _pageModel.OnPostAddClaimAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("SelectedClaimType");
        _pageModel.ModelState["SelectedClaimType"]!.Errors.Single().ErrorMessage
            .Should().Be("Please select a user claim");
        _mockApiScopesAdminService.Verify(
            service => service.AddClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAddClaimAsync_AlreadyApplied_AddsModelError_SelectedClaimType_ExactMessage()
    {
        _pageModel.Id = 22;
        _pageModel.SelectedClaimType = "department";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.AddClaimAsync(22, "department", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddApiScopeClaimResult
            {
                Status = AddApiScopeClaimStatus.AlreadyApplied,
                ClaimType = "department"
            });

        var result = await _pageModel.OnPostAddClaimAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("SelectedClaimType");
        _pageModel.ModelState["SelectedClaimType"]!.Errors.Single().ErrorMessage
            .Should().Be("This user claim is already applied to the API scope.");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_Success_RedirectsAndTempDataUsesTrimmedClaim()
    {
        _pageModel.Id = 23;
        _pageModel.SelectedClaimType = "  department  ";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.AddClaimAsync(23, "  department  ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddApiScopeClaimResult
            {
                Status = AddApiScopeClaimStatus.Success,
                ClaimType = "department"
            });

        var result = await _pageModel.OnPostAddClaimAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(23);
        _pageModel.TempData["Success"].Should().Be("User claim 'department' added successfully");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_MissingScope_ReturnsNotFound()
    {
        _pageModel.Id = 24;
        _pageModel.RemoveClaimType = "department";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(24, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await _pageModel.OnPostRemoveClaimAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_NoSelection_AddsModelError_RemoveClaimType_ExactMessage()
    {
        _pageModel.Id = 25;
        _pageModel.RemoveClaimType = string.Empty;
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var result = await _pageModel.OnPostRemoveClaimAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("RemoveClaimType");
        _pageModel.ModelState["RemoveClaimType"]!.Errors.Single().ErrorMessage
            .Should().Be("Please select a user claim to remove");
        _mockApiScopesAdminService.Verify(
            service => service.RemoveClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_NotApplied_AddsModelError_RemoveClaimType_ExactMessage()
    {
        _pageModel.Id = 26;
        _pageModel.RemoveClaimType = "department";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(26, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.RemoveClaimAsync(26, "department", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveApiScopeClaimResult
            {
                Status = RemoveApiScopeClaimStatus.NotApplied,
                ClaimType = "department"
            });

        var result = await _pageModel.OnPostRemoveClaimAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("RemoveClaimType");
        _pageModel.ModelState["RemoveClaimType"]!.Errors.Single().ErrorMessage
            .Should().Be("The selected user claim is not applied to this API scope.");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_Success_RedirectsAndTempDataUsesTrimmedClaim()
    {
        _pageModel.Id = 27;
        _pageModel.RemoveClaimType = "  department  ";
        _mockApiScopesAdminService
            .Setup(service => service.GetForEditAsync(27, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());
        _mockApiScopesAdminService
            .Setup(service => service.RemoveClaimAsync(27, "  department  ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveApiScopeClaimResult
            {
                Status = RemoveApiScopeClaimStatus.Success,
                ClaimType = "department"
            });

        var result = await _pageModel.OnPostRemoveClaimAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(27);
        _pageModel.TempData["Success"].Should().Be("User claim 'department' removed successfully");
    }
}
