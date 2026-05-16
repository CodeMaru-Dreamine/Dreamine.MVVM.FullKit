using DreamineVMS.Models;
using DreamineVMS.Options;
using Microsoft.Extensions.Options;

namespace DreamineVMS.Services.Cameras;

/// <summary>
/// \brief appsettings.json 기반 카메라 저장소입니다.
/// </summary>
public sealed class ConfigVmsCameraRepository : IVmsCameraRepository
{
    private readonly IReadOnlyList<CameraDevice> _cameras;

    /// <summary>
    /// \brief ConfigVmsCameraRepository 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="options">카메라 설정 옵션입니다.</param>
    public ConfigVmsCameraRepository(IOptions<List<CameraDeviceOptions>> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _cameras = options.Value
            .Where(camera => !string.IsNullOrWhiteSpace(camera.Id))
            .OrderBy(camera => camera.DisplayOrder)
            .Select(camera => new CameraDevice
            {
                Id = camera.Id,
                Name = string.IsNullOrWhiteSpace(camera.Name) ? camera.Id : camera.Name,
                Host = camera.Host,
                RtspUrl = camera.RtspUrl,
                DisplayOrder = camera.DisplayOrder,
                Enabled = camera.Enabled,
                AutoReconnect = camera.AutoReconnect
            })
            .ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<CameraDevice> GetAll() => _cameras;
}
