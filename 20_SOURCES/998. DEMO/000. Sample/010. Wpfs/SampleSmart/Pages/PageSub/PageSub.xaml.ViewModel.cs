using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using Dreamine.MVVM.ViewModels;
using SampleSmart.Pages.PageSub;

namespace SampleSmart.Pages.PageSub
{
    public partial class PageSubViewModel : ViewModelBase
    {
        [DreamineModel]
        public PageSubModel _model;
        [DreamineEvent]
        public PageSubEvent _event;

        [DreamineProperty]
        public string _message = string.Empty;

        /// <summary>
        /// \brief OK 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Ok")]
        private partial void Ok();

        /// <summary>
        /// \brief Cancel 동작 실행.
        /// </summary>
        [DreamineCommand("Event.Cancel")]
        private partial void Cancel();

        public PageSubViewModel()
        {
            _model = null!;
            _event = null!;
            Message = Model.Message;
        }
    }
}
