using Dreamine.Maui.Empty.Events;
using Dreamine.Maui.Empty.Models;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.Maui.Empty.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [DreamineModel] private MainPageModel _model;
    [DreamineEvent] private MainPageEvent _event;
    [DreamineProperty] private string _message = string.Empty;
    [DreamineProperty] private string _statusMessage = string.Empty;

    [DreamineCommand("Event.Ok", BindTo = "StatusMessage")] private partial void Ok();
    [DreamineCommand("Event.Cancel", BindTo = "StatusMessage")] private partial void Cancel();

    public MainPageViewModel(MainPageModel model, MainPageEvent @event)
    {
        _model = model;
        _event = @event;
        Message = Model.Message;
        StatusMessage = Model.StatusMessage;
    }
}
