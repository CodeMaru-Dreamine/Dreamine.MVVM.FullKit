using DreamineVMS.Models;

namespace DreamineVMS.Services.Cameras;

/// <summary>
/// \if KO
/// <para>Camera Repository Change Kind 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera repository change kind functionality and related state.</para>
/// \endif
/// </summary>
public enum CameraRepositoryChangeKind
{
    /// <summary>
    /// \if KO
    /// <para>카메라가 추가되었음을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Indicates that a camera was added.</para>
    /// \endif
    /// </summary>
    Added,

    /// <summary>
    /// \if KO
    /// <para>카메라가 수정되었음을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Indicates that a camera was updated.</para>
    /// \endif
    /// </summary>
    Updated,

    /// <summary>
    /// \if KO
    /// <para>카메라가 삭제되었음을 나타냅니다.</para>
    /// \endif
    /// \if EN
    /// <para>Indicates that a camera was deleted.</para>
    /// \endif
    /// </summary>
    Deleted
}

/// <summary>
/// \if KO
/// <para>Camera Repository Changed Event Args 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates camera repository changed event args functionality and related state.</para>
/// \endif
/// </summary>
public sealed class CameraRepositoryChangedEventArgs : EventArgs
{
    /// <summary>
    /// \if KO
    /// <para>Camera Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the camera id value.</para>
    /// \endif
    /// </summary>
    public required string CameraId { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Kind 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the kind value.</para>
    /// \endif
    /// </summary>
    public required CameraRepositoryChangeKind Kind { get; init; }
    /// <summary>
    /// \if KO
    /// <para>Camera 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the camera value.</para>
    /// \endif
    /// </summary>
    public CameraDevice? Camera { get; init; }
}
