using Dreamine.MVVM.Core;
using Dreamine.MVVM.Attributes;
using DreamineApp.Models.PageSub;
using DreamineApp.Events.PageSub;

namespace DreamineApp.ViewModels.PageSub;

public partial class PageSubViewModel : ViewModelBase
{
	[DreamineModel]
	public PageSubModel _model;
	[DreamineEvent]
	public PageSubEvent _event;

	public string Message => Model.Message;

	[RelayCommand] private void Ok() => Event.Ok();
	[RelayCommand] private void Cancel() => Event.Cancel();
}
