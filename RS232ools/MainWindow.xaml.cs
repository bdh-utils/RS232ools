using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using RS232ools.Serial;

namespace RS232ools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Hosts the serial terminal: connection settings, full-duplex send
    /// (typed text or a streamed file) and receive (live view plus optional
    /// file logging). All serial I/O is delegated to <see cref="SerialPortService"/>;
    /// received data is marshalled back to the UI thread via the Dispatcher.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SerialPortService _serial = new();

        private string? _selectedFilePath;
        private StreamWriter? _logWriter;
        private readonly object _logSync = new();

        // Display options the user can pick for the send line ending.
        private static readonly (string Label, string Value)[] LineEndings =
        {
            ("None", ""),
            ("CR (\\r)", "\r"),
            ("LF (\\n)", "\n"),
            ("CR+LF (\\r\\n)", "\r\n"),
        };

        public MainWindow()
        {
            InitializeComponent();

            _serial.DataReceived += Serial_DataReceived;
            _serial.ErrorOccurred += Serial_ErrorOccurred;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateSettingsCombos();
            RefreshPorts();
            UpdateConnectionUi();
        }

        // ---- Combo population ---------------------------------------------

        private void PopulateSettingsCombos()
        {
            BaudCombo.ItemsSource = new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
            BaudCombo.SelectedItem = 9600;

            DataBitsCombo.ItemsSource = new[] { 5, 6, 7, 8 };
            DataBitsCombo.SelectedItem = 8;

            ParityCombo.ItemsSource = Enum.GetValues(typeof(Parity));
            ParityCombo.SelectedItem = Parity.None;

            StopBitsCombo.ItemsSource = new[] { StopBits.One, StopBits.OnePointFive, StopBits.Two };
            StopBitsCombo.SelectedItem = StopBits.One;

            HandshakeCombo.ItemsSource = Enum.GetValues(typeof(Handshake));
            HandshakeCombo.SelectedItem = Handshake.None;

            LineEndingCombo.ItemsSource = LineEndings;
            LineEndingCombo.DisplayMemberPath = nameof(ValueTuple<string, string>.Item1);
            LineEndingCombo.SelectedIndex = 3; // CR+LF
        }

        private void RefreshPorts()
        {
            var current = PortCombo.SelectedItem as string;
            var ports = SerialPortService.GetAvailablePortNames();
            Array.Sort(ports, StringComparer.OrdinalIgnoreCase);
            PortCombo.ItemsSource = ports;

            if (current is not null && Array.IndexOf(ports, current) >= 0)
            {
                PortCombo.SelectedItem = current;
            }
            else if (ports.Length > 0)
            {
                PortCombo.SelectedIndex = 0;
            }
        }

        private void PortCombo_DropDownOpened(object sender, EventArgs e) => RefreshPorts();

        // ---- Connect / disconnect -----------------------------------------

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (PortCombo.SelectedItem is not string portName || string.IsNullOrWhiteSpace(portName))
            {
                MessageBox.Show(this, "No serial port selected.", "RS232ools",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = new SerialSettings
            {
                PortName = portName,
                BaudRate = (int)BaudCombo.SelectedItem,
                DataBits = (int)DataBitsCombo.SelectedItem,
                Parity = (Parity)ParityCombo.SelectedItem,
                StopBits = (StopBits)StopBitsCombo.SelectedItem,
                Handshake = (Handshake)HandshakeCombo.SelectedItem,
                Encoding = Encoding.ASCII,
            };

            try
            {
                _serial.Open(settings);
            }
            catch (Exception ex) when (ex is SerialPortException or InvalidOperationException)
            {
                MessageBox.Show(this, ex.Message, "Could not connect",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UpdateConnectionUi();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _serial.Close();
            UpdateConnectionUi();
        }

        private void UpdateConnectionUi()
        {
            bool open = _serial.IsOpen;

            ConnectButton.IsEnabled = !open;
            DisconnectButton.IsEnabled = open;
            SendButton.IsEnabled = open;
            SendFileButton.IsEnabled = open && _selectedFilePath is not null;

            // Lock settings while connected.
            PortCombo.IsEnabled = !open;
            BaudCombo.IsEnabled = !open;
            DataBitsCombo.IsEnabled = !open;
            ParityCombo.IsEnabled = !open;
            StopBitsCombo.IsEnabled = !open;
            HandshakeCombo.IsEnabled = !open;

            if (open)
            {
                StatusDot.Fill = (Brush)FindResource("BrandAccent");
                StatusText.Text = $"Connected to {_serial.PortName}";
            }
            else
            {
                StatusDot.Fill = (Brush)FindResource("BrandMuted");
                StatusText.Text = "Disconnected";
            }
        }

        // ---- Sending ------------------------------------------------------

        private async void SendButton_Click(object sender, RoutedEventArgs e) => await SendTypedTextAsync();

        private async void SendBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SendTypedTextAsync();
            }
        }

        private async System.Threading.Tasks.Task SendTypedTextAsync()
        {
            if (!_serial.IsOpen) return;

            string lineEnding = ((ValueTuple<string, string>)LineEndingCombo.SelectedItem).Item2;
            string payload = SendBox.Text + lineEnding;

            try
            {
                await _serial.SendAsync(payload);
                SendBox.Clear();
            }
            catch (Exception ex) when (ex is SerialPortException or InvalidOperationException)
            {
                ReportSendError(ex);
            }
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose a file to stream over the serial port",
                Filter = "All files (*.*)|*.*",
            };

            if (dialog.ShowDialog(this) == true)
            {
                _selectedFilePath = dialog.FileName;
                FilePathText.Text = _selectedFilePath;
                FilePathText.Foreground = (Brush)FindResource("BrandText");
                SendFileButton.IsEnabled = _serial.IsOpen;
            }
        }

        private async void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_serial.IsOpen || _selectedFilePath is null) return;

            byte[] data;
            try
            {
                data = await System.Threading.Tasks.Task.Run(() => File.ReadAllBytes(_selectedFilePath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"Could not read the file: {ex.Message}", "Send file",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SendFileButton.IsEnabled = false;
            try
            {
                await _serial.SendBytesAsync(data);
            }
            catch (Exception ex) when (ex is SerialPortException or InvalidOperationException)
            {
                ReportSendError(ex);
            }
            finally
            {
                SendFileButton.IsEnabled = _serial.IsOpen && _selectedFilePath is not null;
            }
        }

        private void ReportSendError(Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Send failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ---- Receiving ----------------------------------------------------

        private void Serial_DataReceived(object? sender, string text)
        {
            // Raised on a background thread; marshal to the UI thread.
            Dispatcher.BeginInvoke(new Action(() => AppendReceived(text)));
            WriteToLog(text);
        }

        private void Serial_ErrorOccurred(object? sender, Exception ex)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // A read failure usually means the port/cable has gone away.
                // Tear the connection down and surface it in the status bar.
                _serial.Close();
                UpdateConnectionUi();
                StatusDot.Fill = (Brush)FindResource("BrandMuted");
                StatusText.Text = "Disconnected (connection lost)";
            }));
        }

        private void AppendReceived(string text)
        {
            ReceiveBox.AppendText(text);
            if (AutoScrollCheck.IsChecked == true)
            {
                ReceiveBox.ScrollToEnd();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) => ReceiveBox.Clear();

        // ---- Receive logging ----------------------------------------------

        private void LogCheck_Checked(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Log received data toâ€¦",
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"rs232-{DateTime.Now:yyyyMMdd-HHmmss}.log",
            };

            if (dialog.ShowDialog(this) != true)
            {
                LogCheck.IsChecked = false; // user cancelled
                return;
            }

            try
            {
                lock (_logSync)
                {
                    _logWriter = new StreamWriter(dialog.FileName, append: true) { AutoFlush = true };
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"Could not open the log file: {ex.Message}", "Logging",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LogCheck.IsChecked = false;
            }
        }

        private void LogCheck_Unchecked(object sender, RoutedEventArgs e) => CloseLog();

        private void WriteToLog(string text)
        {
            lock (_logSync)
            {
                try
                {
                    _logWriter?.Write(text);
                }
                catch
                {
                    // Don't let a logging failure break the receive path.
                }
            }
        }

        private void CloseLog()
        {
            lock (_logSync)
            {
                _logWriter?.Dispose();
                _logWriter = null;
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
            MaximizeGlyph.Text = maxed ? "" : "";
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
            _serial.Dispose();
            CloseLog();
            base.OnClosing(e);
        }
    }
}
