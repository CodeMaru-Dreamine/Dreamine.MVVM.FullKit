using Dreamine.MVVM.Attributes;
using DreamineApp.Events;
using DreamineApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace DreamineApp.ViewModels
{
	/// <summary>
	/// MainWindow에 대한 ViewModel 클래스입니다.
	/// Model과 Event 사이의 바인딩을 담당합니다.
	/// </summary>
	public partial class MainWindowViewModel
	{
		[DreamineModel]
		private MainWindowModel _model;
		[DreamineEvent]
		private MainWindowEvent _event;

		public string Title => Model.Title;
		public string Message => Model.Message;

		[RelayCommand] private void Ok() => Event.Ok();
		[RelayCommand] private void Cancel() => Event.Cancel();
		[RelayCommand] private void Minimize() => Event.Minimize();
		[RelayCommand] private void Maximize() => Event.Maximize();
		[RelayCommand] private void Close() => Event.Close();
		[RelayCommand] private void SubPage() => Event.SubPage();
	}
}
