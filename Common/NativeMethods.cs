using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InternetMeter.Common;

public static class NativeMethods
{
    public const int HWND_BROADCAST = 0xffff;
    public const int SW_RESTORE = 9;
    public const uint MSGFLT_ADD = 1;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WM_HOTKEY = 0x0312;

    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;
    public const uint VK_G = 0x47;

    public const int GHOST_HOTKEY_ID = 9001;
    public const int INSPECTOR_HOTKEY_ID = 9002;
    public const int REPORT_HOTKEY_ID = 9003;
    public const int DOCK_HOTKEY_ID = 9004;
    public const int ANALYTICS_HOTKEY_ID = 9005;
    public const uint VK_P = 0x50;
    public const uint VK_R = 0x52;
    public const uint VK_D = 0x44;
    public const uint VK_A = 0x41;

    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public const int AF_INET = 2;
    public const int AF_INET6 = 23;
    public const int TCP_TABLE_OWNER_PID_ALL = 5;
    public const int UDP_TABLE_OWNER_PID = 1;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    public struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetProcessIoCounters(IntPtr ProcessHandle, out IO_COUNTERS IoCounters);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        bool bOrder,
        uint ulAf,
        int TableClass,
        uint Reserved = 0);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedUdpTable(
        IntPtr pUdpTable,
        ref int pdwSize,
        bool bOrder,
        uint ulAf,
        int TableClass,
        uint Reserved = 0);

    public static HashSet<int> GetActiveNetworkPids()
    {
        var pids = new HashSet<int>();
        CollectTcpPids(AF_INET, pids);
        CollectTcpPids(AF_INET6, pids);
        CollectUdpPids(AF_INET, pids);
        CollectUdpPids(AF_INET6, pids);
        return pids;
    }

    private static void CollectTcpPids(uint af, HashSet<int> pids)
    {
        int size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, false, af, TCP_TABLE_OWNER_PID_ALL, 0);
        if (size <= 0) return;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(buffer, ref size, false, af, TCP_TABLE_OWNER_PID_ALL, 0) == 0)
            {
                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr currentPtr = IntPtr.Add(buffer, 4);

                int rowSize = (af == AF_INET) ? 24 : 56;
                int pidOffset = (af == AF_INET) ? 20 : 52;

                for (int i = 0; i < numEntries; i++)
                {
                    if (af == AF_INET)
                    {
                        int state = Marshal.ReadInt32(currentPtr, 0);
                        // Skip CLOSED (1) and LISTEN (2) states
                        if (state != 1 && state != 2)
                        {
                            uint remoteAddr = (uint)Marshal.ReadInt32(currentPtr, 12);
                            // Skip unbound (0.0.0.0) and loopback (127.0.0.0/8)
                            if (remoteAddr != 0 && (remoteAddr & 0xFF) != 127)
                            {
                                int pid = Marshal.ReadInt32(currentPtr, pidOffset);
                                if (pid > 0)
                                {
                                    pids.Add(pid);
                                }
                            }
                        }
                    }
                    else
                    {
                        // IPv6
                        int state = Marshal.ReadInt32(currentPtr, 48);
                        if (state != 1 && state != 2)
                        {
                            // Check if remote address is not :: or ::1
                            bool isLoopbackOrZero = true;
                            for (int b = 0; b < 15; b++)
                            {
                                if (Marshal.ReadByte(currentPtr, 24 + b) != 0)
                                {
                                    isLoopbackOrZero = false;
                                    break;
                                }
                            }

                            if (isLoopbackOrZero)
                            {
                                byte lastByte = Marshal.ReadByte(currentPtr, 24 + 15);
                                if (lastByte != 0 && lastByte != 1)
                                {
                                    isLoopbackOrZero = false;
                                }
                            }

                            if (!isLoopbackOrZero)
                            {
                                int pid = Marshal.ReadInt32(currentPtr, pidOffset);
                                if (pid > 0)
                                {
                                    pids.Add(pid);
                                }
                            }
                        }
                    }

                    currentPtr = IntPtr.Add(currentPtr, rowSize);
                }
            }
        }
        catch
        {
            // Silently absorb transient table reallocation errors
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static void CollectUdpPids(uint af, HashSet<int> pids)
    {
        int size = 0;
        GetExtendedUdpTable(IntPtr.Zero, ref size, false, af, UDP_TABLE_OWNER_PID, 0);
        if (size <= 0) return;

        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedUdpTable(buffer, ref size, false, af, UDP_TABLE_OWNER_PID, 0) == 0)
            {
                int numEntries = Marshal.ReadInt32(buffer);
                IntPtr currentPtr = IntPtr.Add(buffer, 4);

                int rowSize = (af == AF_INET) ? 12 : 28;
                int pidOffset = (af == AF_INET) ? 8 : 24;

                for (int i = 0; i < numEntries; i++)
                {
                    if (af == AF_INET)
                    {
                        uint localAddr = (uint)Marshal.ReadInt32(currentPtr, 0);
                        // Skip loopback-bound UDP sockets (127.0.0.0/8)
                        if ((localAddr & 0xFF) != 127)
                        {
                            int pid = Marshal.ReadInt32(currentPtr, pidOffset);
                            if (pid > 0)
                            {
                                pids.Add(pid);
                            }
                        }
                    }
                    else
                    {
                        int pid = Marshal.ReadInt32(currentPtr, pidOffset);
                        if (pid > 0)
                        {
                            pids.Add(pid);
                        }
                    }

                    currentPtr = IntPtr.Add(currentPtr, rowSize);
                }
            }
        }
        catch
        {
            // Silently absorb
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static bool TryGetProcessIoBytes(int pid, out ulong readBytes, out ulong writeBytes)
    {
        readBytes = 0;
        writeBytes = 0;
        if (pid <= 0) return false;

        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == IntPtr.Zero) return false;

        try
        {
            if (GetProcessIoCounters(hProcess, out IO_COUNTERS counters))
            {
                readBytes = counters.ReadTransferCount;
                writeBytes = counters.WriteTransferCount;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    public static readonly int WM_SHOW_INTERNET_METER = RegisterWindowMessage("WM_SHOW_INTERNET_METER_9D4F28A1");

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool ChangeWindowMessageFilter(int message, uint dwFlag);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public static bool SetClickThrough(IntPtr hWnd, bool clickThrough)
    {
        if (hWnd == IntPtr.Zero) return false;
        try
        {
            var currentStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
            var newStyle = clickThrough
                ? (currentStyle | WS_EX_TRANSPARENT)
                : (currentStyle & ~WS_EX_TRANSPARENT);
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, (IntPtr)newStyle);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_AUTO = 0,
        DWMSBT_NONE = 1,
        DWMSBT_MAINWINDOW = 2,      // Mica
        DWMSBT_TRANSIENTWINDOW = 3, // Acrylic (Frosted desktop blur)
        DWMSBT_TABBEDWINDOW = 4     // Mica Alt
    }

    public static bool ApplySystemBackdrop(IntPtr hWnd, DWM_SYSTEMBACKDROP_TYPE backdropType)
    {
        if (hWnd == IntPtr.Zero) return false;
        try
        {
            int backdrop = (int)backdropType;
            int hr = DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
            return hr == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void SignalExistingInstance()
    {
        try
        {
            if (WM_SHOW_INTERNET_METER != 0)
            {
                PostMessage((IntPtr)HWND_BROADCAST, WM_SHOW_INTERNET_METER, IntPtr.Zero, IntPtr.Zero);
            }

            var currentId = Environment.ProcessId;
            var otherProcesses = Process.GetProcessesByName("InternetMeter");
            foreach (var p in otherProcesses)
            {
                if (p.Id != currentId)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(p.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(p.MainWindowHandle);
                    }
                }
            }
        }
        catch
        {
            // Silently absorb
        }
    }
}
