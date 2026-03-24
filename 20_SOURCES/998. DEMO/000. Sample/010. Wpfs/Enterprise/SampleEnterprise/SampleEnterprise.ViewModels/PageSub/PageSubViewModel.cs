using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using SampleEnterprise.Events.PageSub;
using SampleEnterprise.Models.PageSub;

namespace SampleEnterprise.ViewModels.PageSub
{
    public partial class PageSubViewModel 
    {
        [DreamineModel]
        public readonly PageSubModel _model;
        [DreamineEvent]
        public PageSubEvent _event;

        public string Message => Model.Message;

        [RelayCommand] private void Ok() => Event.Ok();
        [RelayCommand] private void Cancel() => Event.Cancel();

        public PageSubViewModel(PageSubModel model, PageSubEvent @event)
        {
            _model = model;
            _event = @event;

            _ = model; 
            _ = @event;
        }
    }
}
