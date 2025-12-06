using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Cheaters.Windows
{
    public class BaseWindow : Window
    {
        public static readonly DependencyProperty ShowTitleBarProperty =
            DependencyProperty.Register("ShowTitleBar", typeof(bool), typeof(BaseWindow), new PropertyMetadata(true));

        public bool ShowTitleBar
        {
            get { return (bool)GetValue(ShowTitleBarProperty); }
            set { SetValue(ShowTitleBarProperty, value); }
        }

        public static readonly DependencyProperty CanMinimizeProperty =
            DependencyProperty.Register("CanMinimize", typeof(bool), typeof(BaseWindow), new PropertyMetadata(true));

        public bool CanMinimize
        {
            get { return (bool)GetValue(CanMinimizeProperty); }
            set { SetValue(CanMinimizeProperty, value); }
        }

        public static readonly DependencyProperty CanMaximizeProperty =
            DependencyProperty.Register("CanMaximize", typeof(bool), typeof(BaseWindow), new PropertyMetadata(true));

        public bool CanMaximize
        {
            get { return (bool)GetValue(CanMaximizeProperty); }
            set { SetValue(CanMaximizeProperty, value); }
        }

        public BaseWindow(ResizeMode _resizeMode = ResizeMode.CanResize)
        {
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.ResizeMode = _resizeMode;

            this.Loaded += BaseWindow_Loaded;
            this.StateChanged += BaseWindow_StateChanged;
        }

        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {

            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            var source = HwndSource.FromHwnd(hwnd);
            source.AddHook(HwndHook);

            // 去除边框
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_DLGMODALFRAME;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // 应用更改
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        private void BaseWindow_StateChanged(object sender, EventArgs e)
        {
            // 当窗口状态改变时，处理最大化状态下的边框问题
            if (this.WindowState == WindowState.Maximized)
            {
                // 获取屏幕工作区大小
                var screenWidth = SystemParameters.WorkArea.Width;
                var screenHeight = SystemParameters.WorkArea.Height;

                // 设置合适的窗口位置和大小
                this.MaxWidth = screenWidth;
                this.MaxHeight = screenHeight + 8; // 补偿标题栏高度
            }
            else
            {
                this.MaxWidth = double.PositiveInfinity;
                this.MaxHeight = double.PositiveInfinity;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTCLIENT = 1;
            const int HTCAPTION = 2;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            // 处理最大化时的边框问题
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_GETMINMAXINFO)
            {
                // 当窗口最大化时，确保标题栏完全可见
                if (this.WindowState == WindowState.Maximized)
                {
                    var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                    // 获取屏幕工作区边界（排除任务栏）
                    var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                    var monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                    GetMonitorInfo(monitor, ref monitorInfo);

                    // 设置窗口位置，确保标题栏不会被遮挡
                    mmi.ptMaxPosition.X = Math.Abs(monitorInfo.rcWork.Left - monitorInfo.rcMonitor.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(monitorInfo.rcWork.Top - monitorInfo.rcMonitor.Top);
                    mmi.ptMaxSize.X = Math.Abs(monitorInfo.rcWork.Right - monitorInfo.rcWork.Left);
                    mmi.ptMaxSize.Y = Math.Abs(monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top);

                    Marshal.StructureToPtr(mmi, lParam, true);
                    handled = true;
                }
            }

            if (msg == WM_NCHITTEST)
            {
                handled = true;
                var mousePos = new Point(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
                var clientPos = PointFromScreen(mousePos);

                // 检查是否在标题栏区域（排除按钮区域）
                if (ShowTitleBar && IsInTitleBar(clientPos) && !IsInControlBoxArea(clientPos))
                {
                    return new IntPtr(HTCAPTION);
                }

                // 检查是否在窗口边缘（用于调整大小）
                var borderThickness = 4; // 边框宽度
                var isLeft = clientPos.X < borderThickness;
                var isRight = clientPos.X > (ActualWidth - borderThickness);
                var isTop = clientPos.Y < borderThickness;
                var isBottom = clientPos.Y > (ActualHeight - borderThickness);

                if (isLeft && isTop) return new IntPtr(HTTOPLEFT);
                if (isRight && isTop) return new IntPtr(HTTOPRIGHT);
                if (isLeft && isBottom) return new IntPtr(HTBOTTOMLEFT);
                if (isRight && isBottom) return new IntPtr(HTBOTTOMRIGHT);
                if (isLeft) return new IntPtr(HTLEFT);
                if (isRight) return new IntPtr(HTRIGHT);
                if (isTop) return new IntPtr(HTTOP);
                if (isBottom) return new IntPtr(HTBOTTOM);

                return new IntPtr(HTCLIENT);
            }

            return IntPtr.Zero;
        }

        private bool IsInTitleBar(Point point)
        {
            // 标题栏高度
            var titleBarHeight = 30;
            return point.Y <= titleBarHeight && point.Y >= 0 && point.X >= 0 && point.X <= ActualWidth;
        }

        // 检查是否在控制按钮区域
        private bool IsInControlBoxArea(Point point)
        {
            // 控制按钮区域 右上角
            var controlBoxWidth = 120; // 三个按钮的宽度
            var controlBoxHeight = 30; // 按钮高度
            return point.X >= (ActualWidth - controlBoxWidth) && point.X <= ActualWidth &&
                   point.Y >= 0 && point.Y <= controlBoxHeight;
        }

        private static int GET_X_LPARAM(IntPtr lp)
        {
            return (short)(lp.ToInt32() & 0xFFFF);
        }

        private static int GET_Y_LPARAM(IntPtr lp)
        {
            return (short)((lp.ToInt32() >> 16) & 0xFFFF);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            // 如果点击在标题栏区域，则允许拖动窗口
            if (ShowTitleBar && IsInTitleBar(e.GetPosition(this)) && !IsInControlBoxArea(e.GetPosition(this)))
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 移除钩子以防止内存泄漏
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(hwnd);
                source?.RemoveHook(HwndHook);
            }

            base.OnClosed(e);
        }



        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            // 如果内容是Grid并且第一行是WindowChrome，则设置标题栏高度
            if (newContent is Grid grid && grid.RowDefinitions.Count > 0)
            {
                if (grid.Children.Count > 0 && grid.Children[0] is WindowChrome)
                {
                    grid.RowDefinitions[0].Height = new GridLength(30);
                }
            }
        }

        #region Win32 API
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
            int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }
        #endregion
    }
}