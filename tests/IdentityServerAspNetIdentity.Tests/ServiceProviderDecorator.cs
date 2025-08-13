namespace IdentityServerAspNetIdentity.Tests;

public class ServiceProviderDecorator : IServiceProvider
{
    private readonly IServiceProvider _originalProvider;

    public ServiceProviderDecorator(IServiceProvider originalProvider)
    {
        _originalProvider = originalProvider ?? throw new ArgumentNullException(nameof(originalProvider));
    }

    public object GetService(Type serviceType)
    {
        // Here, you can intercept the call
        Console.WriteLine($"Requesting service of type: {serviceType.FullName}");

        // Call the original provider
        var service = _originalProvider.GetService(serviceType);

        // You can also modify the result or add additional logic here if needed

        return service;
    }
}