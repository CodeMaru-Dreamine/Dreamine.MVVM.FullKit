namespace DreamineVMS.Services.Streaming;

/// <summary>
/// \brief 카메라 스트림 서비스를 나타냅니다.
/// </summary>
public interface ICameraStreamService
{
    /// <summary>
    /// \brief 지정한 카메라의 HLS 스트림을 시작합니다.
    /// </summary>
    Task StartAsync(string cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief 지정한 카메라의 HLS 스트림을 중지합니다.
    /// </summary>
    Task StopAsync(string cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief 모든 HLS 스트림을 시작합니다.
    /// </summary>
    Task StartAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// \brief 모든 HLS 스트림을 중지합니다.
    /// </summary>
    Task StopAllAsync(CancellationToken cancellationToken = default);
}
