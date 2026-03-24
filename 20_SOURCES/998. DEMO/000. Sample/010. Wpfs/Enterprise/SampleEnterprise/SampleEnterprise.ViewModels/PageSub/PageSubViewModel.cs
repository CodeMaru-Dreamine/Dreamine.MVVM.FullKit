using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleEnterprise.Events.PageSub;
using SampleEnterprise.Models.PageSub;

namespace SampleEnterprise.ViewModels.PageSub
{
    public partial class PageSubViewModel 
    {
        [DreamineModel]
        public PageSubModel _model;
        [DreamineEvent]
        public PageSubEvent _event;

        public string Message => Model.Message;

        [RelayCommand] private static void Ok() => PageSubEvent.Ok();
        [RelayCommand] private static void Cancel() => PageSubEvent.Cancel();

        public PageSubViewModel(PageSubModel model, PageSubEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model;
            _ = @event;
        }
    }
}
