using System;
using System.Runtime.InteropServices;
using Memory;

namespace Cheaters.helper;

public static class Wapi
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        MemHelper.AllocationType flAllocationType,
        Imps.MemoryProtection flProtect
    );
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        MemHelper.FreeType dwFreeType
    );
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtectEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        Imps.MemoryProtection flNewProtect,
        out Imps.MemoryProtection lpflOldProtect);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint SuspendThread(IntPtr hThread);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint ResumeThread(IntPtr hThread);
}