using System.Windows;
using System.Windows.Controls;

namespace Cheaters.Windows
{
    public partial class WindowChrome : UserControl
    {
        public WindowChrome()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                    MaximizeButton.Content = "□";
                }
                else
                {
                    window.WindowState = WindowState.Maximized;
                    MaximizeButton.Content = "❐";
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.Close();
            }
        }
    }
}