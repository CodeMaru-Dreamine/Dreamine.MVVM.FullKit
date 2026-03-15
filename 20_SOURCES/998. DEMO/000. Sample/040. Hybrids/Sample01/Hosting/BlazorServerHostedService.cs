/*!
 * \file BlazorServerHostedService.cs
 * \brief WPF 프로세스 내 Razor Components(.NET 8) 서버 호스팅 (단일 프로세스)
 * \details
 *  - "JS 버튼만 눌리는" 증상은 대개 Blazor 인터랙티브 부트스트랩 실패(특히 /_framework/blazor.web.js 404)로 발생한다.
 *  - WebApplicationOptions.ApplicationName을 BlazorHost(컴포넌트) 어셈블리로 지정해야 Static Web Assets manifest를 올바르게 로딩한다.
 * \author Dreamine
 * \date 2025-12-14
 * \version 1.2.2
 */

using Dreamine.Hybrid.BlazorApp;                 // AppShell, Routes 등
using Dreamine.Hybrid.BlazorApp.ViewModel;       // ViewModel 네임스페이스
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dreamine.Hybrid.Messaging;

namespace Sample01.Hosting
{
	/// <summary>
	/// WPF + Blazor Server(Interactive Server Components) 통합 호스트 서비스
	/// </summary>
	public sealed class BlazorServerHostedService : IHostedService
	{
		/// <summary>내장 Kestrel 앱 인스턴스</summary>
		private IHost? _webHost;

		/*!
 * \file Hosting/BlazorServerHostedService.cs
 * \brief WPF 프로세스 내 Kestrel(5000)로 Razor Components(Interactive Server) 호스팅
 * \details
 *  - /_framework 자산 서빙을 위해 StaticWebAssetsLoader + ApplicationName을 BlazorApp 어셈블리로 고정한다.
 *  - Interactive Server는 Antiforgery 메타데이터가 포함되므로 UseRouting 이후 UseAntiforgery를 배치한다.
 * \author Dreamine
 * \date 2025-12-14
 * \version 1.2.6
 */

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var basePath = AppContext.BaseDirectory;

			var blazorAppAssembly = typeof(Dreamine.Hybrid.BlazorApp.AppShell).Assembly;

			var wb = WebApplication.CreateBuilder(new WebApplicationOptions
			{
				Args = Array.Empty<string>(),
				ContentRootPath = basePath,
				ApplicationName = blazorAppAssembly.GetName().Name
			});

			StaticWebAssetsLoader.UseStaticWebAssets(wb.Environment, wb.Configuration);

			wb.Services
			  .AddRazorComponents()
			  .AddInteractiveServerComponents();

			/*!
             * \details
             *  - WPF/Blazor 간 메시지 전달을 위한 버스 등록.
             *  - Blazor 쪽 ViewModel이 IHybridMessageBus를 생성자에서 요구하므로 반드시 등록되어야 한다.
             *  - Singleton으로 두어 WPF/Blazor가 동일 인스턴스를 공유한다.
             */
			wb.Services.AddSingleton<IHybridMessageBus, InMemoryHybridMessageBus>();

			RegisterViewModels(wb.Services, blazorAppAssembly);

			wb.WebHost.ConfigureKestrel(k => k.ListenLocalhost(5000));

			var app = wb.Build();

			app.UseStaticFiles();
			app.UseRouting();
			app.UseAntiforgery();

			app.MapRazorComponents<Dreamine.Hybrid.BlazorApp.AppShell>()
			   .AddInteractiveServerRenderMode();

			_webHost = app;
			await app.StartAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (_webHost is null) return;
			try { await _webHost.StopAsync(cancellationToken); }
			finally { _webHost.Dispose(); }
		}

		/// <summary>
		/// BlazorHost 어셈블리에서 *ViewModel로 끝나는 public 클래스를 자동 등록합니다.
		/// </summary>
		/// <param name="services">서비스 컬렉션</param>
		/// <param name="assembly">스캔 대상 어셈블리</param>
		private static void RegisterViewModels(IServiceCollection services, Assembly assembly)
		{
			foreach (var t in assembly.GetTypes()
									  .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic && t.Name.EndsWith("ViewModel")))
			{
				/*!
				 * \details ViewModel은 보통 사용자/페이지(회로) 단위 상태를 가지므로 Scoped가 적절합니다.
				 */
				services.AddScoped(t);
			}
		}
	}
}
