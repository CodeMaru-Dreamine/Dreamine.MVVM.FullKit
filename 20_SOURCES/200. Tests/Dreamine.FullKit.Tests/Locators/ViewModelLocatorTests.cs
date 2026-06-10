using Dreamine.MVVM.Interfaces.Locators;
using Dreamine.MVVM.Locators;

namespace Dreamine.FullKit.Tests.Locators;

public sealed class ViewModelLocatorTests : IDisposable
{
    public ViewModelLocatorTests()
    {
        ViewModelLocator.Reset();
    }

    public void Dispose()
    {
        ViewModelLocator.Reset();
    }

    [Fact]
    public void Resolve_UsesRegisteredResolverForMappedViewModel()
    {
        var expected = new SampleViewModel();
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new FixedResolver(expected));

        object? resolved = ViewModelLocator.Resolve(typeof(SampleView));

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void Resolve_ThrowsClearMessageWhenResolverIsMissing()
    {
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));

        var exception = Assert.Throws<InvalidOperationException>(
            () => ViewModelLocator.Resolve(typeof(SampleView)));

        Assert.Contains("no ViewModel resolver is configured", exception.Message);
        Assert.DoesNotContain("Activator.CreateInstance", exception.Message);
    }

    [Fact]
    public void Resolve_ThrowsClearMessageWhenResolverReturnsNull()
    {
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new NullResolver());

        var exception = Assert.Throws<InvalidOperationException>(
            () => ViewModelLocator.Resolve(typeof(SampleView)));

        Assert.Contains("not resolved by the configured resolver", exception.Message);
    }

    [Fact]
    public void Reset_ClearsMappingsAndResolver()
    {
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new FixedResolver(new SampleViewModel()));

        ViewModelLocator.Reset();

        Assert.Null(ViewModelLocator.Resolve(typeof(SampleView)));
    }

    [Fact]
    public void Clear_KeepsResolverAndClearsMappings()
    {
        var expected = new SampleViewModel();
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new FixedResolver(expected));

        ViewModelLocator.Clear();
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));

        object? resolved = ViewModelLocator.Resolve(typeof(SampleView));

        Assert.Same(expected, resolved);
    }

    private sealed class SampleView
    {
    }

    private sealed class SampleViewModel
    {
    }

    private sealed class FixedResolver : IViewModelResolver
    {
        private readonly object _instance;

        public FixedResolver(object instance)
        {
            _instance = instance;
        }

        public object? Resolve(Type viewModelType)
        {
            return _instance.GetType() == viewModelType ? _instance : null;
        }
    }

    private sealed class NullResolver : IViewModelResolver
    {
        public object? Resolve(Type viewModelType)
        {
            return null;
        }
    }
}
