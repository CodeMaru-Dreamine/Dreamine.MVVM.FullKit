using Dreamine.MVVM.Attributes;
using Dreamine.MVVM.Core;
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

		[RelayCommand] private void Ok() => Event.Ok();
		[RelayCommand] private void Cancel() => Event.Cancel();
	}
}
