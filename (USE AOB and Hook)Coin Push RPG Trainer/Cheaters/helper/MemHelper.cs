using System;
using System.Text;

namespace Cheaters.helper;

public static class MemHelper
{
    [Flags]
    public enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }
    
    [Flags]
    public enum FreeType
    {
        Decommit = 0x4000,
        Release = 0x8000
    }

    public static string FormBytes(string bytestr)
    {
        if (string.IsNullOrEmpty(bytestr))
            return string.Empty;
    
        // 转换为大写
        string upperStr = bytestr.ToUpper();
    
        // 如果长度为奇数，在最前面补0
        if (upperStr.Length % 2 == 1)
        {
            upperStr = "0" + upperStr;
        }

        // 每两个字符添加一个空格
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < upperStr.Length; i += 2)
        {
            if (i > 0)
                result.Append(" ");
        
            if (i + 1 < upperStr.Length)
                result.Append(upperStr.Substring(i, 2));
            else
                result.Append(upperStr.Substring(i, 1)); // 处理奇数长度的情况
        }

        return result.ToString();
    }
    /// <summary>
    /// 计算JMP rel32指令的机器码
    /// </summary>
    /// <param name="jmpAddress">JMP指令的起始地址</param>
    /// <param name="targetAddress">要跳转的目标地址</param>
    /// <returns>格式化为"E9 XX XX XX XX"的字符串</returns>
    public static string CalculateJmpRel32(ulong jmpAddress, ulong targetAddress)
    {
        Console.WriteLine($"start{jmpAddress:X} - end{targetAddress:X}");
        // JMP rel32指令长度为5字节
        const int instructionLength = 5;
        
        // 计算下一条指令地址
        ulong nextInstructionAddress = jmpAddress + instructionLength;
        
        // 计算相对位移（有符号32位）
        long displacement = (long)targetAddress - (long)nextInstructionAddress;
        
        // 检查位移是否在32位有符号范围内
        if (displacement < int.MinValue || displacement > int.MaxValue)
        {
            throw new ArgumentException($"位移超出32位有符号范围: {displacement:X}");
        }
        
        // 将位移转换为32位有符号整数，然后转换为小端字节数组
        int rel32 = (int)displacement;
        byte[] bytes = BitConverter.GetBytes(rel32);
        
        // 格式化为"E9 XX XX XX XX"
        return $"E9 {bytes[0]:X2} {bytes[1]:X2} {bytes[2]:X2} {bytes[3]:X2}";
    }
}