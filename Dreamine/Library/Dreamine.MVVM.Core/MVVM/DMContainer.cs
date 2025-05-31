using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Dreamine.MVVM.Core
{
	/// <summary>
	/// 📦 Dreamine 전용 DI 컨테이너 클래스입니다.
	/// 타입별 팩토리 등록, 싱글턴 등록, 자동 등록, 생성자 기반 DI 등을 지원합니다.
	/// </summary>
	public static partial class DMContainer
	{
		private static readonly Dictionary<Type, Func<object>> _map = new();

		/// <summary>
		/// 주어진 타입 T에 대한 팩토리 함수를 등록합니다.
		/// </summary>
		/// <typeparam name="T">등록할 클래스 타입</typeparam>
		/// <param name="factory">생성 함수</param>
		public static void Register<T>(Func<T> factory) where T : class
			=> _map[typeof(T)] = () => factory();

		/// <summary>
		/// 주어진 싱글턴 인스턴스를 타입 T로 등록합니다.
		/// </summary>
		/// <typeparam name="T">등록할 클래스 타입</typeparam>
		/// <param name="instance">싱글턴 인스턴스</param>
		public static void RegisterSingleton<T>(T instance) where T : class
			=> _map[typeof(T)] = () => instance;

		/// <summary>
		/// 타입 T의 인스턴스를 Resolve합니다.
		/// 등록된 팩토리를 통해 생성하며, 없을 경우 예외를 발생시킵니다.
		/// </summary>
		/// <typeparam name="T">해결할 타입</typeparam>
		/// <returns>생성된 인스턴스</returns>
		public static T Resolve<T>() where T : class
		{
			if (_map.TryGetValue(typeof(T), out var factory))
				return (T)factory();
			throw new InvalidOperationException($"[{typeof(T).Name}] 등록되지 않음.");
		}

		/// <summary>
		/// 주어진 타입 인스턴스를 Resolve합니다.
		/// 등록되지 않은 경우 생성자 기반 자동 생성도 시도합니다.
		/// </summary>
		/// <param name="type">타겟 타입</param>
		/// <returns>생성된 인스턴스</returns>
		public static object Resolve(Type type)
		{
			if (_map.TryGetValue(type, out var factory))
				return factory();

			// Fallback: 자동 생성 시도 (등록 안됐지만 생성 가능한 경우)
			if (!type.IsAbstract && type.IsClass)
			{
				var ctor = type.GetConstructors()
					.OrderByDescending(c => c.GetParameters().Length)
					.FirstOrDefault();

				if (ctor != null)
				{
					var args = ctor.GetParameters()
						.Select(p => Resolve(p.ParameterType))
						.ToArray();
					var instance = Activator.CreateInstance(type, args)!;

					// 선택: 한번 생성되면 캐시할 수도 있음
					_map[type] = () => instance;

					return instance;
				}
			}

			throw new InvalidOperationException($"[{type.FullName}] 등록되지 않았고, 생성도 불가능합니다.");
		}


		/// <summary>
		/// 주어진 어셈블리 및 현재 AppDomain 내 어셈블리에서
		/// Model, Event, Manager, ViewModel, View 타입들을 자동 등록합니다.
		/// </summary>
		/// <param name="rootAssembly">우선 처리할 주 어셈블리</param>
		public static void AutoRegisterAll(Assembly rootAssembly)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.FullName))
				.Prepend(rootAssembly) // 우선 대상 Assembly 먼저
				.Distinct();

			foreach (var assembly in assemblies)
			{
				foreach (var type in SafeGetTypes(assembly))
				{
					if (!type.IsClass || type.IsAbstract || type.IsGenericType)
						continue;

					var name = type.Name;

					// DreamineApp + SampleTest 구조 모두 대응
					bool isTarget =
						name.EndsWith("Model") ||
						name.EndsWith("Event") ||
						name.EndsWith("Manager") ||
						name.EndsWith("ViewModel") ||
						name.Contains(".xaml.ViewModel") ||
						name.Contains(".xaml.Model") ||
						name.Contains(".xaml.Event") ||
						(type.IsSubclassOf(typeof(Window)) || type.IsSubclassOf(typeof(System.Windows.Controls.UserControl)));

					if (!isTarget)
						continue;

					if (_map.ContainsKey(type))
						continue;

					// 생성자 우선 등록
					var ctor = type.GetConstructors()
						.OrderByDescending(c => c.GetParameters().Length)
						.FirstOrDefault();

					if (ctor == null)
					{
						// 매개변수 없는 생성자라도 등록 시도
						if (type.GetConstructor(Type.EmptyTypes) != null)
						{
							_map[type] = () => Activator.CreateInstance(type)!;
						}					
						continue;
					}

					_map[type] = () =>
					{
						var args = ctor.GetParameters()
							.Select(p => Resolve(p.ParameterType))
							.ToArray();
						return Activator.CreateInstance(type, args)!;
					};
				}
			}
		}

		/// <summary>
		/// 어셈블리 로드 오류 발생 시에도 가능한 타입을 반환하는 안전한 GetTypes
		/// </summary>
		/// <param name="assembly">대상 어셈블리</param>
		/// <returns>유효한 타입 목록</returns>
		private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where(t => t != null)!;
			}
		}
	}
}
