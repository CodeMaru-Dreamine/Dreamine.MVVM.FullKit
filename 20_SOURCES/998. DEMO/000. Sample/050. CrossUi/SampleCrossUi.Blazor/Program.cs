using Dreamine.UI.Blazor;
using SampleCrossUi.Blazor;
using SampleCrossUi.Blazor.Components;
using SampleCrossUi.Shared.Services;
using SampleCrossUi.Shared.ViewModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register shared services/view models — same ones used by the WPF, WinForms and MAUI samples.
builder.Services.AddScoped<ICounterService, CounterService>();
builder.Services.AddScoped<CounterEvent>();
builder.Services.AddScoped<CounterViewModel>();
builder.Services.AddScoped<LightBulbModel>();
builder.Services.AddScoped<LightBulbEvent>();
builder.Services.AddScoped<LightBulbViewModel>();
builder.Services.AddScoped<ControlsEvent>();
builder.Services.AddScoped<ControlsViewModel>();
builder.Services.AddScoped<DreamineDialogService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
