using Cheaters.Windows;
using System.Windows;
using System.Windows.Media;
using Cheaters.Core;
using Cheaters.helper;
using Cheaters.ViewModels;
using Memory;
using MessageBoxGuo;

namespace Cheaters;

public partial class MainWindow : BaseWindow
{
    private MainViewModel _viewModel;
    private nint processHandle;
    

    // 主题色
    public SolidColorBrush backcolor = new SolidColorBrush(Color.FromRgb(50, 58, 75));
    public SolidColorBrush forecolor = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        InitMem();
    }
    
    
    private async void StartUp_Clicked(object sender, RoutedEventArgs e)
    {
        InitMem();
    }
    
    public void ShowAlert(string message, AlertType type=AlertType.Info, int duration=1000)
    {
        AlertBox2.ShowGlobal(message, type, duration, backcolor, forecolor);
        TextBox_Log.Text+=message+"\n";
        TextBox_Log.ScrollToEnd();
    }

    

    private void ReleaseAllAddress(object sender, RoutedEventArgs e)
    {
        releaseAllAddress();
    }


    private void RecoverAllTasks_Clicked(object sender, RoutedEventArgs e)
    {
        recoverAllTasks();
    }

    private void TestButton4_Clicked(object sender, RoutedEventArgs e)
    {
        foreach (Env.AOBTask aobTask in Env.aobTasks)
        {
            var fminfo = MemoryUtils.FindFreeBlockForRegion(MemMannager.Instance.M.mProc.Handle,
                (nint)aobTask.AOBAdress, (uint)aobTask.InjectCode.Split(" ").Length+5);
            if (fminfo!=IntPtr.Zero | fminfo != null)
            {
                aobTask.FreeAddress = (ulong)fminfo;
                ShowAlert($"搜索空闲地址成功({aobTask.Name}):{fminfo:X}");
            }
            else 
            {
                ShowAlert($"搜索空闲地址失败({aobTask.Name})",AlertType.Error);
            }
        }
    }

    private void TestFirstTask_Clicked(object sender, RoutedEventArgs e)
    {
        applyTask(Env.aobTasks[0]);
    }

    private void ApplyTask_Clicked(object sender, RoutedEventArgs e)
    {
        int index = SelectedTask.SelectedIndex;
        if (index!=-1)
        {
            applyTask(Env.aobTasks[index]);
        }
       
    }
    private void RecoverTask_Clicked(object sender, RoutedEventArgs e)
    {
        int index = SelectedTask.SelectedIndex;
        if (index!=-1)
        {
            recoverTask(Env.aobTasks[index]);
        }
    }
    private void applyTask(Env.AOBTask aobTask)
    {
        Env.AOBTask targetTask = aobTask;
        if (aobTask.isActive)
        {
            ShowAlert($"{aobTask.Name}功能已经激活!");
            return;
        }
        //暂停线程
        Wapi.SuspendThread(MemMannager.Instance.M.mProc.Handle);
        
        var fminfo = MemoryUtils.FindFreeBlockForRegion(MemMannager.Instance.M.mProc.Handle, 
            (nint)targetTask.AOBAdress, 24);
        if (fminfo!=IntPtr.Zero | fminfo != null)
        {
            targetTask.FreeAddress = (ulong)fminfo;
            //开辟内存
            fminfo = fminfo;
            var result=Wapi.VirtualAllocEx(MemMannager.Instance.M.mProc.Handle, fminfo, 24,
                MemHelper.AllocationType.Reserve | MemHelper.AllocationType.Commit,
                Imps.MemoryProtection.ExecuteReadWrite);
            //预处理注入
            string code1 = MemHelper.CalculateJmpRel32((ulong)targetTask.AOBAdress, (ulong)fminfo);
            string code2 = MemHelper.CalculateJmpRel32((ulong)(fminfo + targetTask.InjectCode.Split(" ").Count() ), 
                (ulong)(targetTask.AOBAdress+(ulong)targetTask.AOBCode.Split(" ").Count()));
            

            //AOB处注入
            MemMannager.Instance.M.WriteMemory($"{targetTask.AOBAdress:X}","bytes",$"{code1} {targetTask.FixCode}");
            //注入核心代码
            string rcode = targetTask.InjectCode;
            string[] codeBlock = rcode.Split(" ");
            int length = ((IEnumerable<string>) codeBlock).Count<string>();
            byte[] lpBuffer = new byte[length];
            for (int index = 0; index < length; ++index)
                lpBuffer[index] = Convert.ToByte(codeBlock[index], 16 /*0x10*/);
            //更改保护
            Imps.MemoryProtection oldProtect;
            bool vperesult=Wapi.VirtualProtectEx(MemMannager.Instance.M.mProc.Handle, fminfo, 
                (uint)lpBuffer.Length, 
                Imps.MemoryProtection.ExecuteReadWrite, 
                out oldProtect);
            
            if (!vperesult)
                throw new Exception("更改代码保护失败");
            //注入核心代码
            bool success=MemMannager.Instance.M.WriteMemory($"{fminfo:X}", "bytes", $"{rcode} {code2}","",null ,false);
            
            if (!success)
                throw new Exception("写入内存失败");

            // 恢复原始保护
            Wapi.VirtualProtectEx(MemMannager.Instance.M.mProc.Handle, fminfo,
                (uint)lpBuffer.Length, oldProtect, out _);
            Wapi.ResumeThread(MemMannager.Instance.M.mProc.Handle);
            aobTask.isActive = true;
            ShowAlert("====================\n" +
                      $"功能({targetTask.Name})注入完成:\n" +
                      "====================\n" +
                      $"开辟内存成功:{result:X}\n" +
                      $"AOB地址:{targetTask.AOBAdress:X}\n" +
                      $"注入地址:{targetTask.FreeAddress:X}\n" +
                      $"注入处跳转代码:{code2}\n" +
                      $"AOB处跳转代码:{code1} {targetTask.FixCode}" +
                      $"注入代码:{targetTask.InjectCode}\n" +
                      $"====================");
        }
    }
    
    private void releaseAllAddress()
    {
        foreach (Env.AOBTask aobTask  in Env.aobTasks)
        {
            if ((nint)aobTask.FreeAddress!=IntPtr.Zero)
            {
                if (Wapi.VirtualFreeEx(MemMannager.Instance.M.mProc.Handle, (nint)aobTask.FreeAddress,
                        0, MemHelper.FreeType.Release))
                {
                    ShowAlert($"释放内存成功({aobTask.Name}):{(nint)aobTask.FreeAddress:X}");
                }else
                {
                    ShowAlert($"释放内存失败({aobTask.Name})",AlertType.Error);
                }
            }
        }
    }

    private void releaseAddress(Env.AOBTask aobTask)
    {
        if ((nint)aobTask.FreeAddress!=IntPtr.Zero)
        {
            if (Wapi.VirtualFreeEx(MemMannager.Instance.M.mProc.Handle, (nint)aobTask.FreeAddress,
                    0, MemHelper.FreeType.Release))
            {
                ShowAlert($"释放内存成功({aobTask.Name}):{(nint)aobTask.FreeAddress:X}");
            }else
            {
                ShowAlert($"释放内存失败({aobTask.Name})",AlertType.Error);
            }
        }
    }
    private void recoverAllTasks()
    {
        Wapi.SuspendThread(processHandle);
        releaseAllAddress();
        foreach (Env.AOBTask aobTask in Env.aobTasks)
        {
            var success=MemMannager.Instance.M.WriteMemory(aobTask.AOBAdress.ToString("X"), "bytes", 
                aobTask.AOBCode);
            if (success)
            {
                ShowAlert($"复原功能({aobTask.Name})成功:{aobTask.AOBAdress:X}");    
            }
            else
            {
                ShowAlert($"复原功能({aobTask.Name})失败:{aobTask.AOBAdress:X}",AlertType.Error);
            }
            
        }

        Wapi.ResumeThread(processHandle);
    }
    private void recoverTask(Env.AOBTask aobTask)
    {
        if (!aobTask.isActive)
        {
            ShowAlert($"{aobTask.Name}功能未激活,无需复原!");
        }
        Wapi.SuspendThread(processHandle);
        releaseAddress(aobTask);
        var success=MemMannager.Instance.M.WriteMemory(aobTask.AOBAdress.ToString("X"), "bytes", 
            aobTask.AOBCode);
        if (success)
        {
            ShowAlert($"复原功能({aobTask.Name})成功:{aobTask.AOBAdress:X}");
            aobTask.isActive = false;
        }
        else
        {
            ShowAlert($"复原功能({aobTask.Name})失败:{aobTask.AOBAdress:X}",AlertType.Error);
        }
        Wapi.ResumeThread(processHandle);
    } 
    private async void InitMem()
    {
        if (MemMannager.Instance.M.OpenProcess("CoinPushRPG-Win64-Shipping.exe"))
        {
            ShowAlert("打开程序成功!",AlertType.Success,1500);
            processHandle = MemMannager.Instance.M.mProc.Handle;
        }
        else
        {
            ShowAlert("打开程序失败!",AlertType.Error,1500);
            return;
        }
        Env.aobTasks.Clear();
        foreach (Env.AOBInfo aobInfo in Env.AOBInfos)
        {
            var results=await MemMannager.Instance.M.AoBScan(aobInfo.AOBCode);
            if (results.Count() == 0)
            {
                ShowAlert($"({aobInfo.Name})地址未找到!",AlertType.Error,1500);
            }
            else
            {
                var injectAddress = (ulong)results.FirstOrDefault();
                Env.aobTasks.Add(new Env.AOBTask(){
                    AOBCode = aobInfo.AOBCode,
                    Name = aobInfo.Name,
                    AOBAdress = injectAddress,
                    InjectCode = aobInfo.InjectCode,
                    FixCode = aobInfo.FixCode
                });
                
                ShowAlert($"({aobInfo.Name})已找到:{injectAddress:X}",AlertType.Info,1500);
            }
        }
    }


}