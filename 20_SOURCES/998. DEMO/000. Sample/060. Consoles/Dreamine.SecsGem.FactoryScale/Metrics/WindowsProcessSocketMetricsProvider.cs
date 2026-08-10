using System.Diagnostics;
using System.Runtime.InteropServices;
using Dreamine.SecsGem.FactoryScale.Models;

namespace Dreamine.SecsGem.FactoryScale.Metrics;

/// <summary>
/// Reads PID-owned TCP rows from the Windows extended TCP tables. This is kept
/// outside the SECS Core API and is sampled only at the metrics interval.
/// </summary>
internal sealed class WindowsProcessSocketMetricsProvider : IProcessSocketMetricsProvider
{
    internal static WindowsProcessSocketMetricsProvider Instance { get; } = new();

    private WindowsProcessSocketMetricsProvider() { }

    public FactorySocketMetricSnapshot Capture(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (!OperatingSystem.IsWindows())
            return FactorySocketMetricSnapshot.Unavailable("Windows extended TCP tables are unavailable on this platform.");

        try
        {
            var rows = ReadIpv4(process.Id).Concat(ReadIpv6(process.Id)).ToArray();
            var listeners = rows.Count(value => value.State == TcpState.Listen);
            var established = rows.Count(value => value.State == TcpState.Established);
            var timeWait = rows.Count(value => value.State == TcpState.TimeWait);
            var open = rows.Count(value => value.State is not TcpState.Closed and not TcpState.TimeWait);
            return new FactorySocketMetricSnapshot(open, listeners, established, timeWait,
                FactorySocketMetricSource.OperatingSystemProcessTable, true,
                "Windows GetExtendedTcpTable owner-PID rows (IPv4 + IPv6). Open excludes CLOSED and TIME_WAIT rows.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or ExternalException or DllNotFoundException or EntryPointNotFoundException)
        {
            return FactorySocketMetricSnapshot.Unavailable(
                $"Windows process TCP table sampling failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static IEnumerable<TcpRow> ReadIpv4(int processId)
    {
        foreach (var row in ReadTable<MibTcpRowOwnerPid>(AddressFamily.InterNetwork))
        {
            if (row.OwningPid == processId) yield return new TcpRow((TcpState)row.State);
        }
    }

    private static IEnumerable<TcpRow> ReadIpv6(int processId)
    {
        foreach (var row in ReadTable<MibTcp6RowOwnerPid>(AddressFamily.InterNetworkV6))
        {
            if (row.OwningPid == processId) yield return new TcpRow((TcpState)row.State);
        }
    }

    private static T[] ReadTable<T>(AddressFamily family) where T : struct
    {
        var size = 0;
        var first = GetExtendedTcpTable(IntPtr.Zero, ref size, false, (int)family,
            TcpTableClass.OwnerPidAll, 0);
        if (first is not ErrorInsufficientBuffer and not ErrorSuccess)
            throw new InvalidOperationException($"GetExtendedTcpTable sizing failed with Win32 error {first}.");

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = GetExtendedTcpTable(buffer, ref size, false, (int)family,
                TcpTableClass.OwnerPidAll, 0);
            if (result != ErrorSuccess)
                throw new InvalidOperationException($"GetExtendedTcpTable failed with Win32 error {result}.");

            var count = Marshal.ReadInt32(buffer);
            if (count < 0) throw new InvalidOperationException("GetExtendedTcpTable returned a negative row count.");
            var rowSize = Marshal.SizeOf<T>();
            var requiredBytes = checked(sizeof(int) + count * rowSize);
            if (requiredBytes > size)
                throw new InvalidOperationException("GetExtendedTcpTable returned a truncated row buffer.");
            var row = IntPtr.Add(buffer, sizeof(int));
            var rows = new T[count];
            for (var index = 0; index < count; index++)
                rows[index] = Marshal.PtrToStructure<T>(IntPtr.Add(row, checked(index * rowSize)));
            return rows;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private const uint ErrorSuccess = 0;
    private const uint ErrorInsufficientBuffer = 122;

    private enum AddressFamily
    {
        InterNetwork = 2,
        InterNetworkV6 = 23
    }

    private enum TcpTableClass
    {
        OwnerPidAll = 5
    }

    private enum TcpState : uint
    {
        Closed = 1,
        Listen = 2,
        SynSent = 3,
        SynReceived = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12
    }

    private readonly record struct TcpRow(TcpState State);

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        internal uint State;
        internal uint LocalAddress;
        internal uint LocalPort;
        internal uint RemoteAddress;
        internal uint RemotePort;
        internal int OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] LocalAddress;
        internal uint LocalScopeId;
        internal uint LocalPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] RemoteAddress;
        internal uint RemoteScopeId;
        internal uint RemotePort;
        internal uint State;
        internal int OwningPid;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int size,
        [MarshalAs(UnmanagedType.Bool)] bool order,
        int addressFamily,
        TcpTableClass tableClass,
        uint reserved);
}
