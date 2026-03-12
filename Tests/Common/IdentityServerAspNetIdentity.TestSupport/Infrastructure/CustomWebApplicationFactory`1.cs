using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.TestSupport.Infrastructure;

public class CustomWebApplicationFactory<TAuthHandler> : CustomWebApplicationFactory
    where TAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                foreach (var scheme in options.Schemes)
                {
                    if (scheme.Name == "TestScheme")
                    {
                        scheme.HandlerType = typeof(TAuthHandler);
                    }
                }
            });
        });
    }
}
