using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WeddingPlatform.Models;

namespace WeddingPlatform.Services
{
	/// <summary>
	/// \file JsonGlobalSettingsStore.cs
	/// \brief App_Data/global-settings.json 기반 전체 설정 저장소. 메모리 캐시 + 파일 영속화.
	/// </summary>
	public sealed class JsonGlobalSettingsStore : IGlobalSettingsStore
	{
		private readonly string _path;
		private static readonly SemaphoreSlim _gate = new(1, 1);
		private GlobalSettings? _cache;

		private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

		public JsonGlobalSettingsStore(WeddingOptions opts)
		{
			var dataRoot = Path.GetDirectoryName(opts.ResolvedDataPath.TrimEnd(Path.DirectorySeparatorChar))
				?? Path.Combine(AppContext.BaseDirectory, "App_Data");
			Directory.CreateDirectory(dataRoot);
			_path = Path.Combine(dataRoot, "global-settings.json");
		}

		public async Task<GlobalSettings> GetAsync(CancellationToken ct = default)
		{
			if (_cache is not null) return _cache;

			await _gate.WaitAsync(ct).ConfigureAwait(false);
			try
			{
				if (_cache is not null) return _cache;

				if (!File.Exists(_path))
				{
					_cache = new GlobalSettings();
					return _cache;
				}

				await using var fs = File.OpenRead(_path);
				_cache = await JsonSerializer.DeserializeAsync<GlobalSettings>(fs, _jsonOpts, ct).ConfigureAwait(false)
					?? new GlobalSettings();
				_cache.Normalize();
				return _cache;
			}
			finally { _gate.Release(); }
		}

		public async Task SaveAsync(GlobalSettings settings, CancellationToken ct = default)
		{
			settings.Normalize();
			await _gate.WaitAsync(ct).ConfigureAwait(false);
			try
			{
				var tmp = _path + ".tmp";
				await using (var fs = File.Create(tmp))
					await JsonSerializer.SerializeAsync(fs, settings, _jsonOpts, ct).ConfigureAwait(false);

				File.Copy(tmp, _path, overwrite: true);
				File.Delete(tmp);
				_cache = settings;
			}
			finally { _gate.Release(); }
		}
	}
}
