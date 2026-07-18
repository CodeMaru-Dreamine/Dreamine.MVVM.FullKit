namespace DreamineVMS.Services.Streaming;

/// <summary>
/// \if KO
/// <para>\brief 카메라 스트림 서비스를 나타냅니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates i camera stream service functionality and related state.</para>
/// \endif
/// </summary>
public interface ICameraStreamService
{
    /// <summary>
    /// \if KO
    /// <para>\brief 지정한 카메라의 HLS 스트림을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start async operation.</para>
    /// \endif
    /// </returns>
    Task StartAsync(string cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 지정한 카메라의 HLS 스트림을 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cameraId">
    /// \if KO
    /// <para>camera Id에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for camera id.</para>
    /// \endif
    /// </param>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Stop Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop async operation.</para>
    /// \endif
    /// </returns>
    Task StopAsync(string cameraId, CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 모든 HLS 스트림을 시작합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the start all async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Start All Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the start all async operation.</para>
    /// \endif
    /// </returns>
    Task StartAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// \if KO
    /// <para>\brief 모든 HLS 스트림을 중지합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the stop all async operation.</para>
    /// \endif
    /// </summary>
    /// <param name="cancellationToken">
    /// \if KO
    /// <para>취소 요청을 감시하는 토큰입니다.</para>
    /// \endif
    /// \if EN
    /// <para>A token used to observe cancellation requests.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Stop All Async 작업에서 생성한 <c>Task</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Task</c> result produced by the stop all async operation.</para>
    /// \endif
    /// </returns>
    Task StopAllAsync(CancellationToken cancellationToken = default);
}
