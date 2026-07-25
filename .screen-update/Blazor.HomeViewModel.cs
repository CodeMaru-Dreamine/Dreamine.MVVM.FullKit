using Dreamine.Blazor.Empty.Events;
using Dreamine.Blazor.Empty.Models;
using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;

namespace Dreamine.Blazor.Empty.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [DreamineModel] private HomeModel _model;
    [DreamineEvent] private HomeEvent _event;
    [DreamineProperty] private string _message = string.Empty;
    [DreamineProperty] private string _statusMessage = string.Empty;

    [DreamineCommand("Event.Ok", BindTo = "StatusMessage")]
    private partial void Ok();

    [DreamineCommand("Event.Cancel", BindTo = "StatusMessage")]
    private partial void Cancel();

    public HomeViewModel(HomeModel model, HomeEvent @event)
    {
        _model = model;
        _event = @event;
        Message = Model.Message;
        StatusMessage = Model.StatusMessage;
    }
}
