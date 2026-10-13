using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MyApp.App
{
    /// <summary>
    /// Applies dark theme (black) title bar to WPF windows using the DWM API.
    /// </summary>
    public static class WindowThemeHelper
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>
        /// Enables the dark title bar for the given window.
        /// Call this after the window's handle is created (e.g. in SourceInitialized).
        /// </summary>
        public static void EnableDarkTitleBar(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) => SetDarkMode(new WindowInteropHelper(window).Handle);
                return;
            }

            SetDarkMode(hwnd);
        }

        private static void SetDarkMode(IntPtr hwnd)
        {
            int useDarkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
        }
    }
}