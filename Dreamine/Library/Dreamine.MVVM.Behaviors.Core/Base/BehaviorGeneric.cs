// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dreamine.MVVM.Behaviors.Core.Interfaces;
using System.Diagnostics;
using System.Windows;

namespace Dreamine.MVVM.Behaviors.Core.Base
{
	/// <summary>
	/// 📌 Dreamine의 제네릭 기반 Behavior 클래스입니다.
	/// 
	/// 연결 가능한 대상 객체를 제네릭 타입 <typeparamref name="T"/>로 명확히 지정하여,
	/// <c>AssociatedObject</c>를 타입 캐스팅 없이 직접 사용할 수 있도록 도와줍니다.
	/// 
	/// Freezable을 상속하여 WPF의 리소스 시스템 및 XAML 지원을 보장하며,
	/// IAttachedObject 인터페이스를 통해 Behavior의 연결/해제를 관리합니다.
	/// </summary>
	/// <typeparam name="T">연결할 대상 객체의 타입 (예: Window, Grid, Button 등)</typeparam>
	public abstract class Behavior<T> : Freezable, IAttachedObject where T : DependencyObject
	{
		/// <summary>
		/// 연결된 WPF 객체를 나타냅니다.
		/// Behavior가 Attach되면 이 속성에 대상 객체가 설정됩니다.
		/// </summary>
		public T? AssociatedObject { get; private set; }

		/// <summary>
		/// <see cref="IAttachedObject"/> 인터페이스를 통해 노출되는 AssociatedObject입니다.
		/// </summary>
		DependencyObject IAttachedObject.AssociatedObject => AssociatedObject!;

		/// <summary>
		/// Behavior를 지정한 <paramref name="associatedObject"/>에 연결합니다.
		/// 내부적으로 <c>AssociatedObject</c>에 저장되며, <c>OnAttached()</c>가 호출됩니다.
		/// </summary>
		/// <param name="associatedObject">연결할 DependencyObject (예: Window, Grid 등)</param>
		public void Attach(DependencyObject associatedObject)
		{
			AssociatedObject = (T)associatedObject;
			OnAttached();
		}

		/// <summary>
		/// 현재 연결된 객체와의 연결을 해제합니다.
		/// <c>OnDetaching()</c>이 호출되고, <c>AssociatedObject</c>는 null로 초기화됩니다.
		/// </summary>
		public void Detach()
		{
			OnDetaching();
			AssociatedObject = null;
		}

		/// <summary>
		/// Behavior가 연결되었을 때 호출되는 확장 포인트입니다.
		/// 하위 클래스에서 오버라이딩하여 부가 작업을 수행할 수 있습니다.
		/// </summary>
		protected virtual void OnAttached()
		{			
		}

		/// <summary>
		/// Behavior가 해제될 때 호출되는 확장 포인트입니다.
		/// 하위 클래스에서 오버라이딩하여 정리 작업 등을 수행할 수 있습니다.
		/// </summary>
		protected virtual void OnDetaching()
		{			
		}

		/// <summary>
		/// WPF Freezable 객체를 생성하기 위한 팩토리 메서드입니다.
		/// <c>Activator.CreateInstance()</c>를 사용하므로 기본 생성자가 반드시 필요합니다.
		/// </summary>
		/// <returns>현재 클래스 타입의 새로운 인스턴스</returns>
		protected override Freezable CreateInstanceCore()
		{
			return (Freezable)Activator.CreateInstance(GetType())!;
		}
	}
}
