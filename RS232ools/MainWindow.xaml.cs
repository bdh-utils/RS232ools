using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace RS232ools
{
    /// <summary>
    /// The application shell. Hosts the custom window chrome and a tab strip of
    /// <see cref="SessionView"/> instances â€” one per serial workspace â€” so several
    /// COM ports can be driven (terminal and/or simulator) side by side in a
    /// single window. Each session owns its own port; this class only manages
    /// adding, closing and naming the tabs.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AddSession(); // start with one open workspace
        }

        // ---- Session tabs -------------------------------------------------

        private void NewTabButton_Click(object sender, RoutedEventArgs e) => AddSession();

        private void AddSession()
        {
            var session = new SessionView();
            var header = new SessionTabHeader { Title = session.SuggestedTitle };

            var tab = new TabItem { Header = header, Content = session };

            // Keep the tab label in step with the session's port until the user
            // renames it.
            session.SuggestedTitleChanged += (_, _) => header.SuggestTitle(session.SuggestedTitle);
            header.CloseRequested += (_, _) => CloseSession(tab);

            SessionTabs.Items.Add(tab);
            SessionTabs.SelectedItem = tab;
        }

        private void CloseSession(TabItem tab)
        {
            if (tab.Content is SessionView session)
            {
                session.Shutdown();
            }

            SessionTabs.Items.Remove(tab);

            // Never leave the window with no workspace; reopen a fresh one.
            if (SessionTabs.Items.Count == 0)
            {
                AddSession();
            }
        }

        // ---- Custom window chrome -----------------------------------------

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Swap the maximise glyph for a restore glyph (and vice versa).
            bool maxed = WindowState == WindowState.Maximized;
            MaximizeGlyph.Text = maxed ? "" : ""; // restore / maximise glyphs
            MaximizeButton.ToolTip = maxed ? "Restore" : "Maximise";
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // With WindowStyle=None a maximised window would otherwise cover the
            // taskbar. Hook WM_GETMINMAXINFO to clamp the maximised bounds to the
            // monitor's working area instead.
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            const int MONITOR_DEFAULTTONEAREST = 0x00000002;

            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(monitor, ref info))
                {
                    RECT work = info.rcWork;
                    RECT bounds = info.rcMonitor;

                    // Position/size of the maximised window, relative to the monitor.
                    mmi.ptMaxPosition.X = work.Left - bounds.Left;
                    mmi.ptMaxPosition.Y = work.Top - bounds.Top;
                    mmi.ptMaxSize.X = work.Right - work.Left;
                    mmi.ptMaxSize.Y = work.Bottom - work.Top;

                    // Preserve the window's minimum size (in device pixels).
                    var dpi = VisualTreeHelper.GetDpi(this);
                    mmi.ptMinTrackSize.X = (int)(MinWidth * dpi.DpiScaleX);
                    mmi.ptMinTrackSize.Y = (int)(MinHeight * dpi.DpiScaleY);
                }
            }

            Marshal.StructureToPtr(mmi, lParam, fDeleteOld: true);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        // ---- About / shutdown ---------------------------------------------

        private void AboutLink_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow { Owner = this };
            about.ShowDialog();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Tear down every session's port, log and timers.
            foreach (var item in SessionTabs.Items)
            {
                if (item is TabItem { Content: SessionView session })
                {
                    session.Shutdown();
                }
            }
            base.OnClosing(e);
        }
    }
}
