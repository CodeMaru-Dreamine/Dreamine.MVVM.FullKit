using SampleCrossUi.Blazor;
using SampleCrossUi.Blazor.Components;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register shared services — same ICounterService and CounterViewModel
// used by the WPF and WinForms samples.
builder.Services.AddScoped<ICounterService, CounterService>();
builder.Services.AddScoped<CounterViewModel>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
