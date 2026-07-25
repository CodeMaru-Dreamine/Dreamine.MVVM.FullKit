using Dreamine.Blazor.Empty.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Dreamine.Blazor.Empty.Components.Pages;

public partial class Home
{
    [Inject]
    private HomeViewModel ViewModel { get; set; } = null!;

    private void OnOk()
    {
        ViewModel.OkCommand.Execute(null);
    }

    private void OnCancel()
    {
        ViewModel.CancelCommand.Execute(null);
    }
}
