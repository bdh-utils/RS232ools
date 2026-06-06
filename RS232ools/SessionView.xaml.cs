using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RS232ools.Serial;

namespace RS232ools
{
    /// <summary>
    /// A single serial workspace: connection settings for one COM port plus the
    /// Terminal and Simulator tools that talk over it. Each instance owns its own
    /// <see cref="SerialPortService"/>, so several SessionViews can run different
    /// ports side by side inside one window (see <see cref="MainWindow"/>).
    /// Received data is marshalled back to the UI thread via the Dispatcher.
    /// </summary>
    public partial class SessionView : UserControl
    {
        private readonly SerialPortService _serial = new();

        private string? _selectedFilePath;
        private StreamWriter? _logWriter;
        private readonly object _logSync = new();

        /// <summary>
        /// Raised when the title this session would like its tab to show changes
        /// (currently the selected/open port name). The shell uses it to keep the
        /// tab label in step unless the user has set a custom name.
        /// </summary>
        public event EventHandler? SuggestedTitleChanged;

        /// <summary>A short label for this session — the selected port, or a fallback.</summary>
        public string SuggestedTitle =>
            PortCombo?.SelectedItem as string is { Length: > 0 } port ? port : "Serial";

        // A selectable send line ending. Uses properties (not ValueTuple fields)
        // so WPF data binding can read Label for the dropdown display text.
        private sealed record LineEndingOption(string Label, string Value);

        // Display options the user can pick for the send line ending.
        private static readonly LineEndingOption[] LineEndings =
        {
            new("None", ""),
            new("CR (\\r)", "\r"),
            new("LF (\\n)", "\n"),
            new("CR+LF (\\r\\n)", "\r\n"),
        };

        public SessionView()
        {
            InitializeComponent();

            _serial.DataReceived += Serial_DataReceived;
            _serial.ErrorOccurred += Serial_ErrorOccurred;

            Loaded += SessionView_Loaded;
        }

        // The window that owns this control, used as the parent for dialogs and
        // message boxes. Null only before the control is attached to a window.
        private Window? OwnerWindow => Window.GetWindow(this);

        private bool _loaded;

        private void SessionView_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded can fire again (e.g. when a tab is reselected); only build once.
            if (_loaded) return;
            _loaded = true;

            PopulateSettingsCombos();
            RefreshPorts();
            InitTerminalDisplay();
            InitSimulator();
            InitAdvanced();
            UpdateConnectionUi();

            PortCombo.SelectionChanged += (_, _) => SuggestedTitleChanged?.Invoke(this, EventArgs.Empty);
            SuggestedTitleChanged?.Invoke(this, EventArgs.Empty);
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
            LineEndingCombo.DisplayMemberPath = nameof(LineEndingOption.Label);
            LineEndingCombo.SelectedIndex = 3; // CR+LF

            SendModeCombo.ItemsSource = new[] { "Text", "Hex" };
            SendModeCombo.SelectedIndex = 0;
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
                MessageBox.Show(OwnerWindow, "No serial port selected.", "RS232ools",
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
                MessageBox.Show(OwnerWindow, ex.Message, "Could not connect",
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

            UpdateSimulatorConnectionState(open);
            UpdateAdvancedConnectionState(open);
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

        private bool SendAsHex => SendModeCombo.SelectedIndex == 1;

        private void SendModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Line endings are appended to text sends only; in hex mode the user
            // includes any terminator as bytes (e.g. "0D 0A").
            if (LineEndingCombo is not null)
            {
                LineEndingCombo.IsEnabled = !SendAsHex;
            }
        }

        private async System.Threading.Tasks.Task SendTypedTextAsync()
        {
            if (!_serial.IsOpen) return;

            if (SendAsHex)
            {
                await SendTypedHexAsync();
                return;
            }

            string lineEnding = ((LineEndingOption)LineEndingCombo.SelectedItem).Value;
            string payload = SendBox.Text + lineEnding;

            try
            {
                await _serial.SendAsync(payload);
                TerminalAppendSent(payload);
                SendBox.Clear();
            }
            catch (Exception ex) when (ex is SerialPortException or InvalidOperationException)
            {
                ReportSendError(ex);
            }
        }

        // Parses the send box as hex byte pairs (e.g. "1A 2B FF") and transmits
        // the raw bytes, so arbitrary binary can be sent without a file.
        private async System.Threading.Tasks.Task SendTypedHexAsync()
        {
            if (string.IsNullOrWhiteSpace(SendBox.Text)) return;

            if (!Simulation.HexCodec.TryDecodeToBytes(SendBox.Text, out byte[] bytes))
            {
                MessageBox.Show(OwnerWindow,
                    "Enter binary as hexadecimal byte pairs, e.g. \"1A 2B FF\" or \"1A2BFF\".",
                    "Invalid hex", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (bytes.Length == 0) return;

            try
            {
                await _serial.SendBytesAsync(bytes);
                TerminalAppendSent(Simulation.HexCodec.Encode(bytes) + "\n");
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

            if (dialog.ShowDialog(OwnerWindow) == true)
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
                MessageBox.Show(OwnerWindow, $"Could not read the file: {ex.Message}", "Send file",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SendFileButton.IsEnabled = false;
            try
            {
                await _serial.SendBytesAsync(data);
                EchoSentFile(_selectedFilePath, data);
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
            MessageBox.Show(OwnerWindow, ex.Message, "Send failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ---- Receiving ----------------------------------------------------

        private void Serial_DataReceived(object? sender, string text)
        {
            // Raised on a background thread; marshal to the UI thread.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AppendReceived(text);
                HandleSimulatorIncoming(text);
                HandleResponderIncoming(text);
            }));
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

        private void AppendReceived(string text) => TerminalAppendReceived(text);

        private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearTerminal();

        // ---- Receive logging ----------------------------------------------

        private void LogCheck_Checked(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Log received data to…",
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"rs232-{DateTime.Now:yyyyMMdd-HHmmss}.log",
            };

            if (dialog.ShowDialog(OwnerWindow) != true)
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
                MessageBox.Show(OwnerWindow, $"Could not open the log file: {ex.Message}", "Logging",
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

        // ---- Lifetime -----------------------------------------------------

        /// <summary>True while this session has its port open.</summary>
        public bool IsConnected => _serial.IsOpen;

        /// <summary>
        /// Stops streaming, closes the port and the log, and releases the serial
        /// resources. Called when the tab is closed or the window shuts down.
        /// </summary>
        public void Shutdown()
        {
            _simStreamTimer?.Stop();
            _serial.Dispose();
            CloseLog();
        }
    }
}
