using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DreamineVMS.Services.Streaming;

/// <summary>
/// \brief 자식 프로세스를 Windows Job Object에 묶어, 부모 프로세스가
/// 비정상 종료되더라도 자식 프로세스(ffmpeg)가 함께 종료되도록 보장하는 헬퍼입니다.
/// </summary>
/// <remarks>
/// 부모 프로세스가 정상 경로(StopAsync)로 종료되지 못하고 강제 종료될 때,
/// Job Object에 KILL_ON_JOB_CLOSE 플래그가 설정되어 있으면
/// Job 핸들이 닫히는 순간 OS가 소속 자식 프로세스를 모두 종료합니다.
/// 이를 통해 ffmpeg 고아 프로세스가 남는 문제를 구조적으로 차단합니다.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ChildProcessJob : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>
    /// \brief ChildProcessJob 클래스의 새 인스턴스를 초기화하고 Job Object를 생성합니다.
    /// </summary>
    public ChildProcessJob()
    {
        _handle = CreateJobObject(IntPtr.Zero, null);
        if (_handle == IntPtr.Zero)
        {
            // Job Object를 만들 수 없는 환경(권한 등)에서는 핸들이 0으로 남고,
            // AddProcess는 무시됩니다. 이 경우 기존 동작(개별 Kill)으로 폴백됩니다.
            return;
        }

        JOBOBJECT_BASIC_LIMIT_INFORMATION basicLimit = new()
        {
            LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        };

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION extendedLimit = new()
        {
            BasicLimitInformation = basicLimit
        };

        int length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr extendedPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(extendedLimit, extendedPtr, false);

            if (!SetInformationJobObject(
                    _handle,
                    JobObjectInfoType.ExtendedLimitInformation,
                    extendedPtr,
                    (uint)length))
            {
                CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedPtr);
        }
    }

    /// <summary>
    /// \brief 지정한 프로세스를 Job Object에 추가합니다.
    /// </summary>
    /// <param name="process">Job에 묶을 자식 프로세스입니다.</param>
    /// <returns>성공 여부입니다. Job Object를 사용할 수 없는 환경에서는 false를 반환합니다.</returns>
    public bool AddProcess(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (_handle == IntPtr.Zero || _disposed)
        {
            return false;
        }

        try
        {
            return AssignProcessToJobObject(_handle, process.Handle);
        }
        catch (InvalidOperationException)
        {
            // 프로세스가 이미 종료된 경우 등.
            return false;
        }
    }

    /// <summary>
    /// \brief Job Object 핸들을 닫습니다. KILL_ON_JOB_CLOSE에 의해
    /// 소속된 모든 자식 프로세스가 함께 종료됩니다.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_handle != IntPtr.Zero)
        {
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }

    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

    private enum JobObjectInfoType
    {
        ExtendedLimitInformation = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        JobObjectInfoType infoType,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
