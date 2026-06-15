using DreamineVMS.Web.Models;

namespace DreamineVMS.Web.Services.Cameras;

public enum CameraRepositoryChangeKind { Added, Updated, Deleted }

public sealed class CameraRepositoryChangedEventArgs : EventArgs
{
    public required string CameraId { get; init; }
    public required CameraRepositoryChangeKind Kind { get; init; }
    public CameraDevice? Camera { get; init; }
}
