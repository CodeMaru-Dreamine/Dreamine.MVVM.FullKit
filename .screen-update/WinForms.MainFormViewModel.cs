using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.ViewModels;
using Dreamine.WinForms.Empty.Events;
using Dreamine.WinForms.Empty.Models;

namespace Dreamine.WinForms.Empty.ViewModels;

public partial class MainFormViewModel : ViewModelBase
{
    [DreamineModel] private MainFormModel _model;
    [DreamineEvent] private MainFormEvent _event;
    [DreamineProperty] private string _message = string.Empty;
    [DreamineProperty] private string _statusMessage = string.Empty;

    [DreamineCommand("Event.Ok", BindTo = "StatusMessage")] private partial void Ok();
    [DreamineCommand("Event.Cancel", BindTo = "StatusMessage")] private partial void Cancel();

    public MainFormViewModel(MainFormModel model, MainFormEvent @event)
    {
        _model = model;
        _event = @event;
        Message = Model.Message;
        StatusMessage = Model.StatusMessage;
    }
}
