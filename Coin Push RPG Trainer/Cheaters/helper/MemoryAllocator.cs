using Memory;

namespace Cheaters.helper;

public class MemoryAllocator
{
    // 内存分配的核心方法
    public static IntPtr AllocateMemoryRegion(IntPtr Handle,IntPtr preferredAddress, uint size, 
                                              Imps.MemoryProtection protection)
    {
        IntPtr processHandle = Handle;
        IntPtr allocatedAddress = IntPtr.Zero;
        int maxAttempts = 10;
        
        // 1. 首先尝试在首选地址附近分配
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 搜索空闲内存块
            IntPtr freeBlock = MemoryUtils.FindFreeBlockForRegion(Handle,preferredAddress, size);
            
            if (freeBlock != IntPtr.Zero)
            {
                // 尝试分配
                allocatedAddress = Wapi.VirtualAllocEx(
                    processHandle,
                    freeBlock,
                    size,
                    MemHelper.AllocationType.Reserve | MemHelper.AllocationType.Commit,
                    protection);
                    
                if (allocatedAddress != IntPtr.Zero)
                    break; // 成功分配
            }
            
            // 失败，尝试下一个地址
            preferredAddress = IntPtr.Add(preferredAddress, 65536); // 64KB对齐
        }
        
        // 2. 如果首选地址分配失败，使用系统选择的地址
        if (allocatedAddress == IntPtr.Zero)
        {
            allocatedAddress = Wapi.VirtualAllocEx(
                processHandle,
                IntPtr.Zero, // 让系统选择地址
                size,
                MemHelper.AllocationType.Reserve | MemHelper.AllocationType.Commit,
                protection);
        }
        
        return allocatedAddress;
    }
    
    // 完整的代码注入流程
    public static bool InjectCode(string assemblyScript)
    {
        // 1. 解析脚本，提取alloc命令
        // var allocCommands = ParseAllocCommands(assemblyScript);
        
        // 2. 搜索代码洞穴并分配内存
        // var allocatedMemory = new List<AllocatedBlock>();
        // foreach (var alloc in allocCommands)
        // {
        //     IntPtr address;
        //     
        //     if (alloc.PreferredAddress != IntPtr.Zero)
        //     {
        //         // 尝试在首选地址附近分配
        //         address = FindFreeBlockForRegion(alloc.PreferredAddress, alloc.Size);
        //         if (address == IntPtr.Zero)
        //         {
        //             // 搜索失败，让系统选择地址
        //             address = VirtualAllocEx(processHandle, IntPtr.Zero, alloc.Size,
        //                 MemHelper.AllocationType.RESERVE | MemHelper.AllocationType.COMMIT,
        //                 Imps.MemoryProtection.EXECUTE_READWRITE);
        //         }
        //         else
        //         {
        //             // 在找到的地址分配
        //             address = VirtualAllocEx(processHandle, address, alloc.Size,
        //                 MemHelper.AllocationType.RESERVE | MemHelper.AllocationType.COMMIT,
        //                 Imps.MemoryProtection.EXECUTE_READWRITE);
        //         }
        //     }
        //     else
        //     {
        //         // 无首选地址，让系统选择
        //         address = VirtualAllocEx(processHandle, IntPtr.Zero, alloc.Size,
        //             MemHelper.AllocationType.Reserve | MemHelper.AllocationType.Commit,
        //             Imps.MemoryProtection.ExecuteReadWrite);
        //     }
        //     
        //     if (address == IntPtr.Zero)
        //         throw new Exception($"Failed to allocate memory for {alloc.VarName}");
        //         
        //     allocatedMemory.Add(new AllocatedBlock
        //     {
        //         VarName = alloc.VarName,
        //         Address = address,
        //         Size = alloc.Size
        //     });
        // }
        //
        // 3. 汇编代码并写入分配的内存
        //var assembledCode = AssembleCode(assemblyScript, allocatedMemory);
        

        
        return true;
    }
}