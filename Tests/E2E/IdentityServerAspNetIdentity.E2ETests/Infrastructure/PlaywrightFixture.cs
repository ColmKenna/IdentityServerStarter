using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Infrastructure;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public PlaywrightWebApplicationFactory Factory { get; private set; } = default!;
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;
    public string RootUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Factory = new PlaywrightWebApplicationFactory();
        using var _ = Factory.CreateClient();
        RootUrl = Factory.RootUrl;

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();

        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
    }
}
