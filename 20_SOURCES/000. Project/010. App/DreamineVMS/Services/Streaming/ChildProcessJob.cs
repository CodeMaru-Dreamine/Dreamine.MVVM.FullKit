using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DreamineVMS.Services.Streaming;

/// <summary>
/// \if KO
/// <para>\brief 자식 프로세스를 Windows Job Object에 묶어, 부모 프로세스가 비정상 종료되더라도 자식 프로세스(ffmpeg)가 함께 종료되도록 보장하는 헬퍼입니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates child process job functionality and related state.</para>
/// \endif
/// </summary>
/// <remarks>
/// \if KO
/// <para>부모 프로세스가 정상 경로(StopAsync)로 종료되지 못하고 강제 종료될 때, Job Object에 KILL_ON_JOB_CLOSE 플래그가 설정되어 있으면 Job 핸들이 닫히는 순간 OS가 소속 자식 프로세스를 모두 종료합니다. 이를 통해 ffmpeg 고아 프로세스가 남는 문제를 구조적으로 차단합니다.</para>
/// \endif
/// \if EN
/// <para>Describes behavior and usage considerations for this member.</para>
/// \endif
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ChildProcessJob : IDisposable
{
    /// <summary>
    /// \if KO
    /// <para>handle 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the handle value.</para>
    /// \endif
    /// </summary>
    private IntPtr _handle;
    /// <summary>
    /// \if KO
    /// <para>disposed 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the disposed value.</para>
    /// \endif
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// \if KO
    /// <para>\brief ChildProcessJob 클래스의 새 인스턴스를 초기화하고 Job Object를 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="ChildProcessJob"/> class with the specified settings.</para>
    /// \endif
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
    /// \if KO
    /// <para>\brief 지정한 프로세스를 Job Object에 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the process item.</para>
    /// \endif
    /// </summary>
    /// <param name="process">
    /// \if KO
    /// <para>Job에 묶을 자식 프로세스입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Process</c> value used for process.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>성공 여부입니다. Job Object를 사용할 수 없는 환경에서는 false를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the add process condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
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
    /// \if KO
    /// <para>\brief Job Object 핸들을 닫습니다. KILL_ON_JOB_CLOSE에 의해 소속된 모든 자식 프로세스가 함께 종료됩니다.</para>
    /// \endif
    /// \if EN
    /// <para>Releases resources owned by this instance.</para>
    /// \endif
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

    /// <summary>
    /// \if KO
    /// <para>JOB OBJECT LIMIT KILL ON JOB CLOSE 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the job object limit kill on job close value.</para>
    /// \endif
    /// </summary>
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

    /// <summary>
    /// \if KO
    /// <para>Job Object Info Type 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates job object info type functionality and related state.</para>
    /// \endif
    /// </summary>
    private enum JobObjectInfoType
    {
        /// <summary>
        /// \if KO
        /// <para>Extended Limit Information 값을 나타냅니다.</para>
        /// \endif
        /// \if EN
        /// <para>Represents the extended limit information value.</para>
        /// \endif
        /// </summary>
        ExtendedLimitInformation = 9
    }

    /// <summary>
    /// \if KO
    /// <para>JOBOBJECT BASIC LIMIT INFORMATION 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates jobobject basic limit information functionality and related state.</para>
    /// \endif
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        /// <summary>
        /// \if KO
        /// <para>Per Process User Time Limit 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the per process user time limit value.</para>
        /// \endif
        /// </summary>
        public long PerProcessUserTimeLimit;
        /// <summary>
        /// \if KO
        /// <para>Per Job User Time Limit 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the per job user time limit value.</para>
        /// \endif
        /// </summary>
        public long PerJobUserTimeLimit;
        /// <summary>
        /// \if KO
        /// <para>Limit Flags 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the limit flags value.</para>
        /// \endif
        /// </summary>
        public uint LimitFlags;
        /// <summary>
        /// \if KO
        /// <para>Minimum Working Set Size 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the minimum working set size value.</para>
        /// \endif
        /// </summary>
        public UIntPtr MinimumWorkingSetSize;
        /// <summary>
        /// \if KO
        /// <para>Maximum Working Set Size 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the maximum working set size value.</para>
        /// \endif
        /// </summary>
        public UIntPtr MaximumWorkingSetSize;
        /// <summary>
        /// \if KO
        /// <para>Active Process Limit 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the active process limit value.</para>
        /// \endif
        /// </summary>
        public uint ActiveProcessLimit;
        /// <summary>
        /// \if KO
        /// <para>Affinity 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the affinity value.</para>
        /// \endif
        /// </summary>
        public UIntPtr Affinity;
        /// <summary>
        /// \if KO
        /// <para>Priority Class 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the priority class value.</para>
        /// \endif
        /// </summary>
        public uint PriorityClass;
        /// <summary>
        /// \if KO
        /// <para>Scheduling Class 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the scheduling class value.</para>
        /// \endif
        /// </summary>
        public uint SchedulingClass;
    }

    /// <summary>
    /// \if KO
    /// <para>IO COUNTERS 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates io counters functionality and related state.</para>
    /// \endif
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        /// <summary>
        /// \if KO
        /// <para>Read Operation Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the read operation count value.</para>
        /// \endif
        /// </summary>
        public ulong ReadOperationCount;
        /// <summary>
        /// \if KO
        /// <para>Write Operation Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the write operation count value.</para>
        /// \endif
        /// </summary>
        public ulong WriteOperationCount;
        /// <summary>
        /// \if KO
        /// <para>Other Operation Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the other operation count value.</para>
        /// \endif
        /// </summary>
        public ulong OtherOperationCount;
        /// <summary>
        /// \if KO
        /// <para>Read Transfer Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the read transfer count value.</para>
        /// \endif
        /// </summary>
        public ulong ReadTransferCount;
        /// <summary>
        /// \if KO
        /// <para>Write Transfer Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the write transfer count value.</para>
        /// \endif
        /// </summary>
        public ulong WriteTransferCount;
        /// <summary>
        /// \if KO
        /// <para>Other Transfer Count 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the other transfer count value.</para>
        /// \endif
        /// </summary>
        public ulong OtherTransferCount;
    }

    /// <summary>
    /// \if KO
    /// <para>JOBOBJECT EXTENDED LIMIT INFORMATION 기능과 관련 상태를 캡슐화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Encapsulates jobobject extended limit information functionality and related state.</para>
    /// \endif
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        /// <summary>
        /// \if KO
        /// <para>Basic Limit Information 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the basic limit information value.</para>
        /// \endif
        /// </summary>
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        /// <summary>
        /// \if KO
        /// <para>Io Info 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the io info value.</para>
        /// \endif
        /// </summary>
        public IO_COUNTERS IoInfo;
        /// <summary>
        /// \if KO
        /// <para>Process Memory Limit 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the process memory limit value.</para>
        /// \endif
        /// </summary>
        public UIntPtr ProcessMemoryLimit;
        /// <summary>
        /// \if KO
        /// <para>Job Memory Limit 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the job memory limit value.</para>
        /// \endif
        /// </summary>
        public UIntPtr JobMemoryLimit;
        /// <summary>
        /// \if KO
        /// <para>Peak Process Memory Used 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the peak process memory used value.</para>
        /// \endif
        /// </summary>
        public UIntPtr PeakProcessMemoryUsed;
        /// <summary>
        /// \if KO
        /// <para>Peak Job Memory Used 값을 보관합니다.</para>
        /// \endif
        /// \if EN
        /// <para>Stores the peak job memory used value.</para>
        /// \endif
        /// </summary>
        public UIntPtr PeakJobMemoryUsed;
    }

    /// <summary>
    /// \if KO
    /// <para>Job Object 값을 생성합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Creates the job object value.</para>
    /// \endif
    /// </summary>
    /// <param name="lpJobAttributes">
    /// \if KO
    /// <para>lp Job Attributes에 사용할 <c>IntPtr</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> value used for lp job attributes.</para>
    /// \endif
    /// </param>
    /// <param name="lpName">
    /// \if KO
    /// <para>lp Name에 사용할 <c>string?</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string?</c> value used for lp name.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Create Job Object 작업에서 생성한 <c>IntPtr</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> result produced by the create job object operation.</para>
    /// \endif
    /// </returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    /// <summary>
    /// \if KO
    /// <para>Information Job Object 값을 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Sets the information job object value.</para>
    /// \endif
    /// </summary>
    /// <param name="hJob">
    /// \if KO
    /// <para>h Job에 사용할 <c>IntPtr</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> value used for h job.</para>
    /// \endif
    /// </param>
    /// <param name="infoType">
    /// \if KO
    /// <para>info Type에 사용할 <c>JobObjectInfoType</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>JobObjectInfoType</c> value used for info type.</para>
    /// \endif
    /// </param>
    /// <param name="lpJobObjectInfo">
    /// \if KO
    /// <para>lp Job Object Info에 사용할 <c>IntPtr</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> value used for lp job object info.</para>
    /// \endif
    /// </param>
    /// <param name="cbJobObjectInfoLength">
    /// \if KO
    /// <para>cb Job Object Info Length에 사용할 <c>uint</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>uint</c> value used for cb job object info length.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Set Information Job Object 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the set information job object condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        JobObjectInfoType infoType,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength);

    /// <summary>
    /// \if KO
    /// <para>Assign Process To Job Object 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the assign process to job object operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hJob">
    /// \if KO
    /// <para>h Job에 사용할 <c>IntPtr</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> value used for h job.</para>
    /// \endif
    /// </param>
    /// <param name="hProcess">
    /// \if KO
    /// <para>h Process에 사용할 <c>IntPtr</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> value used for h process.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Assign Process To Job Object 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the assign process to job object condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    /// <summary>
    /// \if KO
    /// <para>Close Handle 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the close handle operation.</para>
    /// \endif
    /// </summary>
    /// <param name="hObject">
    /// \if KO
    /// <para>h Object에 사용할 <c>IntPtr</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IntPtr</c> value used for h object.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Close Handle 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the close handle condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
