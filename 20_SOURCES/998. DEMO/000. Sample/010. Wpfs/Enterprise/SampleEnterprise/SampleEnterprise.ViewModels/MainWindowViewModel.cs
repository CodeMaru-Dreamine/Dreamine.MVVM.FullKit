using Dreamine.MVVM.Attributes;
using SampleEnterprise.Events;
using SampleEnterprise.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SampleEnterprise.ViewModels
{
    /// <summary>
    /// MainWindow에 대한 ViewModel 클래스입니다.
    /// Model과 Event 사이의 바인딩을 담당합니다.
    /// </summary>
    public partial class MainWindowViewModel
    {
        [DreamineModel]
        private readonly MainWindowModel _model;
        [DreamineEvent]
        private readonly MainWindowEvent _event;

        public string Title => Model.Title;
        public string Message => Model.Message;

        [RelayCommand] private static void Ok() => MainWindowEvent.Ok();
        [RelayCommand] private static void Cancel() => MainWindowEvent.Cancel();
        [RelayCommand] private static void Minimize() => MainWindowEvent.Minimize();
        [RelayCommand] private static void Maximize() => MainWindowEvent.Maximize();
        [RelayCommand] private static void Close() => MainWindowEvent.Close();
        [RelayCommand] private void SubPage() => Event.SubPage();

        public MainWindowViewModel(MainWindowModel model, MainWindowEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model;
            _ = @event;
        }
    }
}
