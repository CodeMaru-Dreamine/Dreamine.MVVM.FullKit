using System.Reflection;
using Dreamine.MVVM.Core.AutoRegistration;
using Dreamine.MVVM.Core.DependencyInjection;
using Dreamine.MVVM.Interfaces.DependencyInjection;

namespace Dreamine.FullKit.Tests.Core;

/// <summary>
/// \if KO
/// <para>Auto Registration Tests 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates auto registration tests functionality and related state.</para>
/// \endif
/// </summary>
public sealed class AutoRegistrationTests
{
    /// <summary>
    /// \if KO
    /// <para>Naming Convention Filter Identifies Supported Types 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the naming convention filter identifies supported types operation.</para>
    /// \endif
    /// </summary>
    /// <param name="type">
    /// \if KO
    /// <para>type에 사용할 <c>Type</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Type</c> value used for type.</para>
    /// \endif
    /// </param>
    /// <param name="expected">
    /// \if KO
    /// <para>expected에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for expected.</para>
    /// \endif
    /// </param>
    [Theory]
    [InlineData(typeof(CustomerModel), true)]
    [InlineData(typeof(CustomerEvent), true)]
    [InlineData(typeof(CustomerManager), false)]
    [InlineData(typeof(ExplicitlyRegisteredUtility), true)]
    [InlineData(typeof(CustomerViewModel), true)]
    [InlineData(typeof(CustomerService), false)]
    public void NamingConventionFilter_IdentifiesSupportedTypes(Type type, bool expected)
    {
        Assert.Equal(expected, new NamingConventionAutoRegistrationFilter().IsTarget(type));
    }

    /// <summary>
    /// \if KO
    /// <para>Auto Registration Service Registers Matching Types As Singletons 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the auto registration service registers matching types as singletons operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Single Assembly Scanner 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates single assembly scanner functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class SingleAssemblyScanner : IAssemblyTypeScanner
    {
        /// <summary>
        /// \if KO
        /// <para>assembly 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the assembly value.</para>
        /// \endif
        /// </summary>
        private readonly Assembly _assembly;

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="SingleAssemblyScanner"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="SingleAssemblyScanner"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="assembly">
        /// \if KO
        /// <para>assembly에 사용할 <c>Assembly</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Assembly</c> value used for assembly.</para>
        /// \endif
        /// </param>
        public SingleAssemblyScanner(Assembly assembly)
        {
            _assembly = assembly;
        }

        /// <summary>
        /// \if KO
        /// <para>Candidate Assemblies 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the candidate assemblies value.</para>
        /// \endif
        /// </summary>
        /// <param name="rootAssembly">
        /// \if KO
        /// <para>root Assembly에 사용할 <c>Assembly</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Assembly</c> value used for root assembly.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Get Candidate Assemblies 작업에서 생성한 <c>IEnumerable&lt;Assembly&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IEnumerable&lt;Assembly&gt;</c> result produced by the get candidate assemblies operation.</para>
        /// \endif
        /// </returns>
        public IEnumerable<Assembly> GetCandidateAssemblies(Assembly rootAssembly)
        {
            yield return _assembly;
        }

        /// <summary>
        /// \if KO
        /// <para>Loadable Types 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the loadable types value.</para>
        /// \endif
        /// </summary>
        /// <param name="assembly">
        /// \if KO
        /// <para>assembly에 사용할 <c>Assembly</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>Assembly</c> value used for assembly.</para>
        /// \endif
        /// </param>
        /// <returns>
        /// \if KO
        /// <para>Get Loadable Types 작업에서 생성한 <c>IEnumerable&lt;Type&gt;</c> 결과입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>IEnumerable&lt;Type&gt;</c> result produced by the get loadable types operation.</para>
        /// \endif
        /// </returns>
        public IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            return assembly.GetTypes();
        }
    }

    /// <summary>
    /// \if KO
    /// <para>Customer Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates customer model functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class CustomerModel
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Customer Event 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates customer event functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class CustomerEvent
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Customer Manager 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates customer manager functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class CustomerManager
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Explicitly Registered Utility 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates explicitly registered utility functionality and related state.</para>
    /// \endif
    /// </summary>
    [DreamineRegister]
    public sealed class ExplicitlyRegisteredUtility
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Customer View Model 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates customer view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class CustomerViewModel
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Customer Service 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates customer service functionality and related state.</para>
    /// \endif
    /// </summary>
    public sealed class CustomerService
    {
    }

    /// <summary>
    /// \if KO
    /// <para>Dreamine Register Attribute 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates dreamine register attribute functionality and related state.</para>
    /// \endif
    /// </summary>
    private sealed class DreamineRegisterAttribute : Attribute
    {
    }
}
