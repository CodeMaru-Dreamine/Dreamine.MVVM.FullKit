using System.Reflection;
using Dreamine.MVVM.Core.AutoRegistration;
using Dreamine.MVVM.Core.DependencyInjection;
using Dreamine.MVVM.Interfaces.DependencyInjection;

namespace Dreamine.FullKit.Tests.Core;

public sealed class AutoRegistrationTests
{
    [Theory]
    [InlineData(typeof(CustomerModel), true)]
    [InlineData(typeof(CustomerEvent), true)]
    [InlineData(typeof(CustomerManager), true)]
    [InlineData(typeof(CustomerViewModel), true)]
    [InlineData(typeof(CustomerService), false)]
    public void NamingConventionFilter_IdentifiesSupportedTypes(Type type, bool expected)
    {
        Assert.Equal(expected, new NamingConventionAutoRegistrationFilter().IsTarget(type));
    }

    [Fact]
    public void AutoRegistrationService_RegistersMatchingTypesAsSingletons()
    {
        var container = new DreamineContainer();
        var service = new AutoRegistrationService(
            new SingleAssemblyScanner(typeof(CustomerModel).Assembly),
            new NamingConventionAutoRegistrationFilter());

        service.RegisterAll(typeof(CustomerModel).Assembly, container);

        Assert.True(container.IsRegistered(typeof(CustomerModel)));
        Assert.Same(container.Resolve<CustomerModel>(), container.Resolve<CustomerModel>());
        Assert.False(container.IsRegistered(typeof(CustomerService)));
    }

    private sealed class SingleAssemblyScanner : IAssemblyTypeScanner
    {
        private readonly Assembly _assembly;

        public SingleAssemblyScanner(Assembly assembly)
        {
            _assembly = assembly;
        }

        public IEnumerable<Assembly> GetCandidateAssemblies(Assembly rootAssembly)
        {
            yield return _assembly;
        }

        public IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            return assembly.GetTypes();
        }
    }

    public sealed class CustomerModel
    {
    }

    public sealed class CustomerEvent
    {
    }

    public sealed class CustomerManager
    {
    }

    public sealed class CustomerViewModel
    {
    }

    public sealed class CustomerService
    {
    }
}
