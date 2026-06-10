using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
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
    public partial class MainWindowViewModel : ViewModelBase
    {
        [DreamineModel]
        private MainWindowModel _model;
        [DreamineEvent]
        private MainWindowEvent _event;

        public string Title => Model.Title;
        public string Message => Model.Message;

        [DreamineCommand] private void Ok() => Event.Ok();
        [DreamineCommand] private void Cancel() => Event.Cancel();
        [DreamineCommand] private void Minimize() => Event.Minimize();
        [DreamineCommand] private void Maximize() => Event.Maximize();
        [DreamineCommand] private void Close() => Event.Close();
        [DreamineCommand] private void SubPage() => Event.SubPage();

        public MainWindowViewModel(MainWindowModel model, MainWindowEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model;
            _ = @event;
        }
    }
}
