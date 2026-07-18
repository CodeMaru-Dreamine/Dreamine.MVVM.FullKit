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
    /// \if KO
    /// <para>MainWindow에 대한 ViewModel 클래스입니다. Model과 Event 사이의 바인딩을 담당합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates main window view model functionality and related state.</para>
    /// \endif
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// \if KO
        /// <para>model 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the model value.</para>
        /// \endif
        /// </summary>
        [DreamineModel]
        private MainWindowModel _model;
        /// <summary>
        /// \if KO
        /// <para>event 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the event value.</para>
        /// \endif
        /// </summary>
        [DreamineEvent]
        private MainWindowEvent _event;

        /// <summary>
        /// \if KO
        /// <para>Title 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the title value.</para>
        /// \endif
        /// </summary>
        public string Title => Model.Title;
        /// <summary>
        /// \if KO
        /// <para>Message 값을 가져옵니다.</para>
        /// \endif
        /// \if EN
        /// <para>Gets the message value.</para>
        /// \endif
        /// </summary>
        public string Message => Model.Message;

        /// <summary>
        /// \if KO
        /// <para>Ok 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the ok operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Ok() => Event.Ok();
        /// <summary>
        /// \if KO
        /// <para>Cancel 조건을 확인합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Determines whether cancel.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Cancel() => Event.Cancel();
        /// <summary>
        /// \if KO
        /// <para>Minimize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the minimize operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Minimize() => Event.Minimize();
        /// <summary>
        /// \if KO
        /// <para>Maximize 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the maximize operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Maximize() => Event.Maximize();
        /// <summary>
        /// \if KO
        /// <para>Close 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the close operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void Close() => Event.Close();
        /// <summary>
        /// \if KO
        /// <para>Sub Page 작업을 수행합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Performs the sub page operation.</para>
        /// \endif
        /// </summary>
        [DreamineCommand] private void SubPage() => Event.SubPage();

        /// <summary>
        /// \if KO
        /// <para>지정한 설정으로 <see cref="MainWindowViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Initializes a new instance of the <see cref="MainWindowViewModel"/> class with the specified settings.</para>
        /// \endif
        /// </summary>
        /// <param name="model">
        /// \if KO
        /// <para>model에 사용할 <c>MainWindowModel</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>MainWindowModel</c> value used for model.</para>
        /// \endif
        /// </param>
        /// <param name="event">
        /// \if KO
        /// <para>event에 사용할 <c>MainWindowEvent</c> 값입니다.</para>
        /// \endif
        /// \if EN
        /// <para>The <c>MainWindowEvent</c> value used for event.</para>
        /// \endif
        /// </param>
        public MainWindowViewModel(MainWindowModel model, MainWindowEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model;
            _ = @event;
        }
    }
}
