using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace Cheaters;
public static class MemoryUtils
{
    // 内存基本信息结构
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public IntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    // 系统信息结构
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        ushort reserved;
        public uint pageSize;
        public IntPtr minimumApplicationAddress;
        public IntPtr maximumApplicationAddress;
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    // 常量定义
    private const uint MEM_FREE = 0x10000;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;

    /// <summary>
    /// 在指定基址附近搜索足够大的空闲内存区域
    /// </summary>
    /// <param name="baseAddress">首选基址</param>
    /// <param name="size">需要的内存大小</param>
    /// <returns>找到的空闲内存地址，如果没有找到则返回IntPtr.Zero</returns>
    public static IntPtr FindFreeBlockForRegion(IntPtr Handle,IntPtr baseAddress, uint size)
    {
        // 获取当前进程句柄（示例中使用当前进程）
        IntPtr processHandle =Handle;
        // 获取系统信息
        SYSTEM_INFO systemInfo = GetSystemInfo();

        // 如果基址为0，直接返回null
        if (baseAddress == IntPtr.Zero)
            return IntPtr.Zero;

        ulong baseValue = (ulong)baseAddress;
        ulong minAddress;
        ulong maxAddress;

        // 计算搜索范围：基址上下各0x70000000
        if (baseValue > 0x70000000)
            minAddress = baseValue - 0x70000000;
        else
            minAddress = 0x10000;

        if (baseValue + 0x70000000 > baseValue)
            maxAddress = baseValue + 0x70000000;
        else
            maxAddress = 0xFFFFFFFFFFFF0000; // 64位最大地址

        // 32位和64位处理
        if (Is64BitProcess())
        {
            // 检查是否是网络连接
            // 这里简化处理，实际应检查网络连接状态
            // if (IsNetworkConnection()) ...
            
            // 限制在应用程序地址范围内
            ulong minAppAddr = (ulong)systemInfo.minimumApplicationAddress;
            ulong maxAppAddr = (ulong)systemInfo.maximumApplicationAddress;
            
            if (minAddress < minAppAddr)
                minAddress = minAppAddr;
            if (minAddress > maxAppAddr)
                minAddress = minAppAddr;
                
            if (maxAddress < minAppAddr)
                maxAddress = minAppAddr;
            if (maxAddress > maxAppAddr)
                maxAddress = maxAppAddr;
        }
        else
        {
            // 32位系统使用固定的地址范围
            minAddress = 0x10000;
            maxAddress = 0xFFFFFFFF;
        }

        // 开始搜索
        IntPtr result = IntPtr.Zero;
        ulong currentAddress = minAddress;
        ulong allocationGranularity = systemInfo.allocationGranularity;

        while (true)
        {
            // 查询内存信息
            MEMORY_BASIC_INFORMATION mbi = new MEMORY_BASIC_INFORMATION();
            int queryResult = VirtualQueryEx(
                processHandle,
                (IntPtr)currentAddress,
                out mbi,
                (uint)Marshal.SizeOf(mbi));

            if (queryResult == 0)
                break; // 查询失败或到达内存末尾

            // 检查是否超出搜索范围
            ulong regionEnd = (ulong)mbi.BaseAddress + (ulong)mbi.RegionSize;
            if ((ulong)mbi.BaseAddress > maxAddress)
                break; // 超出搜索范围

            // 检查是否是空闲内存且大小足够
            if (mbi.State == MEM_FREE && (ulong)mbi.RegionSize >= size)
            {
                ulong candidateAddress = (ulong)mbi.BaseAddress;

                // 检查地址对齐
                if (candidateAddress % allocationGranularity > 0)
                {
                    // 需要对齐到分配粒度
                    ulong offset = allocationGranularity - (candidateAddress % allocationGranularity);
                    
                    // 检查对齐后是否有足够空间
                    if ((ulong)mbi.RegionSize - offset >= size)
                    {
                        candidateAddress += offset;

                        // 如果候选地址在基址之前，尝试调整到靠近基址的位置
                        if (candidateAddress < baseValue)
                        {
                            // 尝试从区域末尾开始分配
                            ulong endAddress = candidateAddress + ((ulong)mbi.RegionSize - offset) - size;
                            if (endAddress > baseValue)
                                candidateAddress = baseValue;

                            // 对齐到分配粒度
                            candidateAddress -= candidateAddress % allocationGranularity;
                        }

                        // 选择最接近基址的候选地址
                        if (result == IntPtr.Zero ||
                            Math.Abs((long)candidateAddress - (long)baseValue) < 
                            Math.Abs((long)result - (long)baseValue))
                        {
                            result = (IntPtr)candidateAddress;
                        }
                    }
                }
                else
                {
                    // 已经对齐的内存区域
                    if (candidateAddress < baseValue)
                    {
                        // 从区域末尾开始分配
                        candidateAddress = candidateAddress + (ulong)mbi.RegionSize - size;
                        if (candidateAddress > baseValue)
                            candidateAddress = baseValue;

                        // 对齐到分配粒度
                        candidateAddress -= candidateAddress % allocationGranularity;
                    }

                    // 选择最接近基址的候选地址
                    if (result == IntPtr.Zero ||
                        Math.Abs((long)candidateAddress - (long)baseValue) < 
                        Math.Abs((long)result - (long)baseValue))
                    {
                        result = (IntPtr)candidateAddress;
                    }
                }
            }

            // 移动到下一个内存区域
            ulong nextAddress = (ulong)mbi.BaseAddress + (ulong)mbi.RegionSize;
            
            // 确保对齐到分配粒度
            if ((ulong)mbi.RegionSize % allocationGranularity > 0)
            {
                nextAddress += allocationGranularity - ((ulong)mbi.RegionSize % allocationGranularity);
            }

            // 检查是否超出搜索范围或溢出
            if (nextAddress > maxAddress)
                break;
            if (nextAddress <= currentAddress) // 溢出检查
                break;

            currentAddress = nextAddress;
        }

        return result;
    }

    /// <summary>
    /// 获取系统信息
    /// </summary>
    private static SYSTEM_INFO GetSystemInfo()
    {
        SYSTEM_INFO sysInfo = new SYSTEM_INFO();
        GetSystemInfo(ref sysInfo);
        return sysInfo;
    }

    /// <summary>
    /// 检查当前进程是否是64位
    /// </summary>
    private static bool Is64BitProcess()
    {
        return IntPtr.Size == 8;
    }

    // P/Invoke 声明
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int VirtualQueryEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer,
        uint dwLength);

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect);
}