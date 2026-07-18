using Dreamine.MVVM.Interfaces.Locators;
using Dreamine.MVVM.Locators;

namespace Dreamine.FullKit.Tests.Locators;

/// <summary>
/// \if KO
/// <para>View Model Locator Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates view model locator tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ViewModelLocatorTests : IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="ViewModelLocatorTests"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ViewModelLocatorTests"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    public ViewModelLocatorTests()
    {
        ViewModelLocator.Reset();
    }

    /// <summary>
    /// \if KO
    /// <para>이 인스턴스가 소유한 리소스를 해제합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
    /// </summary>
    public void Dispose()
    {
        ViewModelLocator.Reset();
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Uses Registered Resolver For Mapped View Model 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve uses registered resolver for mapped view model operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Resolve_UsesRegisteredResolverForMappedViewModel()
    {
        var expected = new SampleViewModel();
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new FixedResolver(expected));

        object? resolved = ViewModelLocator.Resolve(typeof(SampleView));

        Assert.Same(expected, resolved);
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Throws Clear Message When Resolver Is Missing 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve throws clear message when resolver is missing operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Resolve_ThrowsClearMessageWhenResolverIsMissing()
    {
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));

        var exception = Assert.Throws<InvalidOperationException>(
            () => ViewModelLocator.Resolve(typeof(SampleView)));

        Assert.Contains("no ViewModel resolver is configured", exception.Message);
        Assert.DoesNotContain("Activator.CreateInstance", exception.Message);
    }

    /// <summary>
    /// \if KO
    /// <para>Resolve Throws Clear Message When Resolver Returns Null 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the resolve throws clear message when resolver returns null operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Resolve_ThrowsClearMessageWhenResolverReturnsNull()
    {
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new NullResolver());

        var exception = Assert.Throws<InvalidOperationException>(
            () => ViewModelLocator.Resolve(typeof(SampleView)));

        Assert.Contains("not resolved by the configured resolver", exception.Message);
    }

    /// <summary>
    /// \if KO
    /// <para>Reset Clears Mappings And Resolver 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the reset clears mappings and resolver operation.</para>
    /// \endif
    /// </summary>
    [Fact]
    public void Reset_ClearsMappingsAndResolver()
    {
        ViewModelLocator.Register(typeof(SampleView), typeof(SampleViewModel));
        ViewModelLocator.RegisterResolver(new FixedResolver(new SampleViewModel()));

        ViewModelLocator.Reset();

        Assert.Null(ViewModelLocator.Resolve(typeof(SampleView)));
    }

    /// <summary>
    /// \if KO
    /// <para>Clear Keeps Resolver And Clears Mappings 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the clear keeps resolver and clears mappings operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Sample View 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates sample view functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class SampleView
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Sample View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates sample view model functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class SampleViewModel
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Fixed Resolver 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates fixed resolver functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class FixedResolver : IViewModelResolver
    {
        /// <summary>
        /// \if KO
        /// <para>instance 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the instance value.</para>
        /// \endif
        /// </summary>
        private readonly object _instance;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="FixedResolver"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="FixedResolver"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="instance">
        /// \if KO
        /// <para>instance에 사용할 <c>object</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>object</c> value used for instance.</para>
        /// \endif
        /// </param>
        public FixedResolver(object instance)
        {
            _instance = instance;
        }

        /// <summary>
        /// \if KO
        /// <para>Resolve 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the resolve operation.</para>
        /// \endif
        /// </summary>
        /// <param name="viewModelType">
        /// \if KO
        /// <para>view Model Type에 사용할 <c>Type</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Type</c> value used for view model type.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Resolve 작업에서 생성한 <c>object?</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>object?</c> result produced by the resolve operation.</para>
        /// \endif
        /// </returns>
        public object? Resolve(Type viewModelType)
        {
            return _instance.GetType() == viewModelType ? _instance : null;
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Null Resolver 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates null resolver functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class NullResolver : IViewModelResolver
    {
        /// <summary>
        /// \if KO
        /// <para>Resolve 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the resolve operation.</para>
        /// \endif
        /// </summary>
        /// <param name="viewModelType">
        /// \if KO
        /// <para>view Model Type에 사용할 <c>Type</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Type</c> value used for view model type.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Resolve 작업에서 생성한 <c>object?</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>object?</c> result produced by the resolve operation.</para>
        /// \endif
        /// </returns>
        public object? Resolve(Type viewModelType)
        {
            return null;
        }
    }
}
