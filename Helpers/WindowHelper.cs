using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VolumeWatcher.Helpers
{
    public static class WindowHelper
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void HideWindow()
        {
            try
            {
                IntPtr h = Process.GetCurrentProcess().MainWindowHandle;
                ShowWindow(h, 0);
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteErrorLine("Failed hiding Window", ex);
            }
        }
    }
}
