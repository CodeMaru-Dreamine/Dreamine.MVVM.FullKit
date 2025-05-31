using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
using SampleCore.Events.PageSub;
using SampleCore.Models.PageSub;

namespace SampleCore.ViewModels.PageSub
{
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
}
