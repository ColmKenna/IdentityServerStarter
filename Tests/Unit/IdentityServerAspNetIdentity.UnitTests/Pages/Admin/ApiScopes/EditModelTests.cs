using IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.ApiScopes;

public class EditModelTests
{
    private readonly Mock<IApiScopesAdminService> _mockApiScopesAdminService = new();

    [Fact]
    public async Task OnGetAsync_ShouldPopulatePageData_WhenCreateMode()
    {
        var sut = CreateSut();
        var pageData = CreatePageData(
            name: string.Empty,
            displayName: null,
            description: null,
            enabled: true,
            appliedClaims: [],
            availableClaims: ["email", "role"]);

        _mockApiScopesAdminService.Setup(x => x.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        sut.Input.Name.Should().BeEmpty();
        sut.Input.Enabled.Should().BeTrue();
        sut.AppliedUserClaims.Should().BeEmpty();
        sut.AvailableUserClaims.Should().Equal("email", "role");
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenEditScopeMissing()
    {
        var sut = CreateSut();
        sut.Id = 42;

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulatePageData_WhenEditScopeExists()
    {
        var sut = CreateSut();
        sut.Id = 42;
        var pageData = CreatePageData(
            name: "orders.read",
            displayName: "Orders",
            description: "Read orders",
            enabled: false,
            appliedClaims: ["role"],
            availableClaims: ["email"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        sut.Input.Name.Should().Be("orders.read");
        sut.Input.DisplayName.Should().Be("Orders");
        sut.Input.Description.Should().Be("Read orders");
        sut.Input.Enabled.Should().BeFalse();
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email");
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageWithoutCallingCreate_WhenCreateModelStateInvalid()
    {
        var sut = CreateSut();
        sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Orders",
            Description = "Read orders",
            Enabled = true
        };
        sut.ModelState.AddModelError("Input.Name", "Required");

        var pageData = CreatePageData(name: string.Empty);
        _mockApiScopesAdminService.Setup(x => x.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _mockApiScopesAdminService.Verify(
            x => x.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldAddModelStateError_WhenCreateDuplicateName()
    {
        var sut = CreateSut();
        sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Orders",
            Description = "Read orders",
            Enabled = true
        };

        var pageData = CreatePageData(name: string.Empty);
        _mockApiScopesAdminService.Setup(x => x.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        _mockApiScopesAdminService.Setup(x => x.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiScopeResult.DuplicateName());

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey("Input.Name");
        sut.ModelState["Input.Name"]!.Errors.Should().ContainSingle(e => e.ErrorMessage == "An API scope with this name already exists.");
    }

    [Fact]
    public async Task OnPostAsync_ShouldCreateScopeAndRedirect_WhenCreateSucceeds()
    {
        var sut = CreateSut();
        sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Orders",
            Description = "Read orders",
            Enabled = false
        };

        CreateApiScopeRequest? capturedRequest = null;
        _mockApiScopesAdminService.Setup(x => x.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiScopeResult.Success(17))
            .Callback<CreateApiScopeRequest, CancellationToken>((request, _) => capturedRequest = request);

        var result = await sut.OnPostAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(17);
        sut.TempData["Success"].Should().Be("API scope created successfully");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Name.Should().Be("orders.read");
        capturedRequest.DisplayName.Should().Be("Orders");
        capturedRequest.Description.Should().Be("Read orders");
        capturedRequest.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenEditScopeMissing()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.Input = new EditModel.ApiScopeInputModel { Name = "orders.read" };

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
        _mockApiScopesAdminService.Verify(
            x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldRehydrateClaimsAndPreserveInput_WhenEditModelStateInvalid()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "edited-name",
            DisplayName = "Edited display",
            Description = "Edited description",
            Enabled = false
        };
        sut.ModelState.AddModelError("Input.Name", "Required");

        var existingData = CreatePageData(
            name: "stored-name",
            displayName: "Stored display",
            description: "Stored description",
            enabled: true,
            appliedClaims: ["role"],
            availableClaims: ["email", "location"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        sut.Input.Name.Should().Be("edited-name");
        sut.Input.DisplayName.Should().Be("Edited display");
        sut.Input.Description.Should().Be("Edited description");
        sut.Input.Enabled.Should().BeFalse();
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email", "location");
    }

    [Fact]
    public async Task OnPostAsync_ShouldRehydrateClaimsAndAddError_WhenEditDuplicateName()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "edited-name",
            DisplayName = "Edited display",
            Description = "Edited description",
            Enabled = false
        };

        var existingData = CreatePageData(
            name: "stored-name",
            displayName: "Stored display",
            description: "Stored description",
            enabled: true,
            appliedClaims: ["role"],
            availableClaims: ["email", "location"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);
        _mockApiScopesAdminService.Setup(x => x.UpdateAsync(9, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateApiScopeResult.DuplicateName());

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey("Input.Name");
        sut.Input.Name.Should().Be("edited-name");
        sut.Input.DisplayName.Should().Be("Edited display");
        sut.Input.Description.Should().Be("Edited description");
        sut.Input.Enabled.Should().BeFalse();
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email", "location");
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenUpdateReturnsNotFound()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.Input = new EditModel.ApiScopeInputModel { Name = "orders.read" };

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: "orders.read"));
        _mockApiScopesAdminService.Setup(x => x.UpdateAsync(9, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateApiScopeResult.NotFound());

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldUpdateScopeAndRedirect_WhenEditSucceeds()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "orders.write",
            DisplayName = "Orders Write",
            Description = "Write orders",
            Enabled = true
        };

        UpdateApiScopeRequest? capturedRequest = null;
        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: "orders.read"));
        _mockApiScopesAdminService.Setup(x => x.UpdateAsync(9, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateApiScopeResult.Success())
            .Callback<int, UpdateApiScopeRequest, CancellationToken>((_, request, _) => capturedRequest = request);

        var result = await sut.OnPostAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(9);
        sut.TempData["Success"].Should().Be("API scope updated successfully");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Name.Should().Be("orders.write");
        capturedRequest.DisplayName.Should().Be("Orders Write");
        capturedRequest.Description.Should().Be("Write orders");
        capturedRequest.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnNotFound_WhenScopeMissing()
    {
        var sut = CreateSut();
        sut.Id = 9;

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await sut.OnPostAddClaimAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldRehydrateAndAddError_WhenClaimSelectionMissing()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.SelectedClaimType = " ";
        var existingData = CreatePageData(
            name: "orders.read",
            appliedClaims: ["role"],
            availableClaims: ["email"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        var result = await sut.OnPostAddClaimAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey(nameof(EditModel.SelectedClaimType));
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email");
        _mockApiScopesAdminService.Verify(
            x => x.AddClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnNotFound_WhenAddClaimReturnsNotFound()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.SelectedClaimType = "email";

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: "orders.read"));
        _mockApiScopesAdminService.Setup(x => x.AddClaimAsync(9, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddApiScopeClaimResult.NotFound());

        var result = await sut.OnPostAddClaimAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldRehydrateAndAddError_WhenClaimAlreadyApplied()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.SelectedClaimType = "email";
        var existingData = CreatePageData(
            name: "orders.read",
            appliedClaims: ["role"],
            availableClaims: ["email"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);
        _mockApiScopesAdminService.Setup(x => x.AddClaimAsync(9, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddApiScopeClaimResult.AlreadyApplied("email"));

        var result = await sut.OnPostAddClaimAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey(nameof(EditModel.SelectedClaimType));
        sut.ModelState[nameof(EditModel.SelectedClaimType)]!.Errors.Should().ContainSingle(
            e => e.ErrorMessage == "This user claim is already applied to the API scope.");
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldRedirectWithSuccess_WhenClaimAdded()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.SelectedClaimType = "email";

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: "orders.read"));
        _mockApiScopesAdminService.Setup(x => x.AddClaimAsync(9, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddApiScopeClaimResult.Success("email"));

        var result = await sut.OnPostAddClaimAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(9);
        sut.TempData["Success"].Should().Be("User claim 'email' added successfully");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnNotFound_WhenScopeMissing()
    {
        var sut = CreateSut();
        sut.Id = 9;

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        var result = await sut.OnPostRemoveClaimAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldRehydrateAndAddError_WhenClaimSelectionMissing()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.RemoveClaimType = " ";
        var existingData = CreatePageData(
            name: "orders.read",
            appliedClaims: ["role"],
            availableClaims: ["email"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        var result = await sut.OnPostRemoveClaimAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey(nameof(EditModel.RemoveClaimType));
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email");
        _mockApiScopesAdminService.Verify(
            x => x.RemoveClaimAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnNotFound_WhenRemoveClaimReturnsNotFound()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.RemoveClaimType = "role";

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: "orders.read"));
        _mockApiScopesAdminService.Setup(x => x.RemoveClaimAsync(9, "role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveApiScopeClaimResult.NotFound());

        var result = await sut.OnPostRemoveClaimAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldRehydrateAndAddError_WhenClaimNotApplied()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.RemoveClaimType = "role";
        var existingData = CreatePageData(
            name: "orders.read",
            appliedClaims: ["role"],
            availableClaims: ["email"]);

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);
        _mockApiScopesAdminService.Setup(x => x.RemoveClaimAsync(9, "role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveApiScopeClaimResult.NotApplied("role"));

        var result = await sut.OnPostRemoveClaimAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey(nameof(EditModel.RemoveClaimType));
        sut.ModelState[nameof(EditModel.RemoveClaimType)]!.Errors.Should().ContainSingle(
            e => e.ErrorMessage == "The selected user claim is not applied to this API scope.");
        sut.AppliedUserClaims.Should().Equal("role");
        sut.AvailableUserClaims.Should().Equal("email");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldRedirectWithSuccess_WhenClaimRemoved()
    {
        var sut = CreateSut();
        sut.Id = 9;
        sut.RemoveClaimType = "role";

        _mockApiScopesAdminService.Setup(x => x.GetForEditAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(name: "orders.read"));
        _mockApiScopesAdminService.Setup(x => x.RemoveClaimAsync(9, "role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveApiScopeClaimResult.Success("role"));

        var result = await sut.OnPostRemoveClaimAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(9);
        sut.TempData["Success"].Should().Be("User claim 'role' removed successfully");
    }

    private EditModel CreateSut()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);

        return new EditModel(_mockApiScopesAdminService.Object)
        {
            PageContext = new PageContext(actionContext) { ViewData = viewData },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private static ApiScopeEditPageDataDto CreatePageData(
        string name,
        string? displayName = "Orders",
        string? description = "Scope description",
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
            AppliedUserClaims = appliedClaims ?? [],
            AvailableUserClaims = availableClaims ?? []
        };
    }
}
