using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace IdentityServerAspNetIdentity.Tests;

public class MockableTagHelperFactoryDecorator : ITagHelperFactory
{
    private readonly ITagHelperFactory _innerFactory;
    private readonly Dictionary<Type, ITagHelper> _mocks;

    public MockableTagHelperFactoryDecorator(ITagHelperFactory innerFactory)
    {
        _innerFactory = innerFactory ?? throw new ArgumentNullException(nameof(innerFactory));
        _mocks = new Dictionary<Type, ITagHelper>();
    }

    public void SetMock<TTagHelper>(ITagHelper mock) where TTagHelper : ITagHelper
    {
        if (mock == null) throw new ArgumentNullException(nameof(mock));

        _mocks[typeof(TTagHelper)] = mock;
    }

    public TTagHelper CreateTagHelper<TTagHelper>(ViewContext context) where TTagHelper : ITagHelper
    {
        if (_mocks.TryGetValue(typeof(TTagHelper), out var mock))
        {
            return (TTagHelper)mock;
        }

        return _innerFactory.CreateTagHelper<TTagHelper>(context);
    }
}