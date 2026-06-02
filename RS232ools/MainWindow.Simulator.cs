using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RS232ools.Serial;
using RS232ools.Simulation;

namespace RS232ools
{
    /// <summary>
    /// The "Simulator" tab: generate random, user-customisable CSV / NMEA strings
    /// (one-shot or streamed at an interval) over the open port, and parse the
    /// same kinds of strings out of the incoming data into a table. All format
    /// and value logic lives in the WPF-free <see cref="Simulation"/> layer; this
    /// partial just wires it to the controls and the shared serial service.
    /// </summary>
    public partial class MainWindow
    {
        private readonly ObservableCollection<FieldDefinition> _simFields = new();
        private readonly MessageFormat _simFormat = new();
        private readonly MessageGenerator _simGenerator = new();
        private DispatcherTimer? _simStreamTimer;
        private int _simSentCount;

        private DataTable? _simParsedTable;
        private readonly StringBuilder _simRxBuffer = new();
        private bool _simParseEnabled;

        // Guards control event handlers that fire during InitializeComponent,
        // before the simulator state and all its controls exist.
        private bool _simReady;

        // ---- Setup --------------------------------------------------------

        private void InitSimulator()
        {
            SimFormatCombo.ItemsSource = Enum.GetValues(typeof(MessageFormatKind));
            SimFormatCombo.SelectedItem = MessageFormatKind.Csv;

            SimFieldTypeColumn.ItemsSource = Enum.GetValues(typeof(FieldType));

            // A sensible starter format so the tab is useful immediately.
            _simFields.Add(new FieldDefinition { Name = "id", Type = FieldType.FixedText, FixedValue = "SENSOR" });
            _simFields.Add(new FieldDefinition { Name = "seq", Type = FieldType.IncrementingCounter, Min = 1 });
            _simFields.Add(new FieldDefinition { Name = "temp", Type = FieldType.RandomDecimal, Min = 15, Max = 30, Precision = 2 });
            _simFields.Add(new FieldDefinition { Name = "status", Type = FieldType.RandomInteger, Min = 0, Max = 1 });
            _simFormat.Fields = _simFields;
            SimFieldsGrid.ItemsSource = _simFields;

            _simStreamTimer = new DispatcherTimer();
            _simStreamTimer.Tick += SimStreamTimer_Tick;

            _simReady = true;
            UpdateFormatFromUi();
            UpdatePreview();
            UpdateSimulatorConnectionState(_serial.IsOpen);
        }

        /// <summary>Enables/disables the send controls with the connection.</summary>
        private void UpdateSimulatorConnectionState(bool open)
        {
            if (!_simReady) return;
            SimGenerateSendButton.IsEnabled = open;
            SimStreamCheck.IsEnabled = open;
            if (!open) StopStreaming();
        }

        // ---- Format controls ----------------------------------------------

        private void SimFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFormatFromUi();
            UpdatePreview();
        }

        private void SimDelimiter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFormatFromUi();
            UpdatePreview();
        }

        private void SimChecksum_Changed(object sender, RoutedEventArgs e)
        {
            UpdateFormatFromUi();
            UpdatePreview();
        }

        private void UpdateFormatFromUi()
        {
            if (!_simReady) return;

            _simFormat.Kind = SimFormatCombo.SelectedItem is MessageFormatKind kind
                ? kind
                : MessageFormatKind.Csv;
            // Delimiter is stored as typed; the generator/parser apply the comma
            // fallback for CSV/NMEA. Plain ignores it entirely.
            _simFormat.Delimiter = SimDelimiterBox.Text;
            _simFormat.IncludeChecksum = SimChecksumCheck.IsChecked == true;

            // The checksum option only applies to NMEA; Plain has no separator.
            SimChecksumCheck.IsEnabled = _simFormat.Kind == MessageFormatKind.Nmea;
            SimDelimiterBox.IsEnabled = _simFormat.Kind != MessageFormatKind.Plain;
        }

        // ---- Field editing ------------------------------------------------

        private void SimAddField_Click(object sender, RoutedEventArgs e)
        {
            _simFields.Add(new FieldDefinition { Name = "field" + (_simFields.Count + 1) });
        }

        private void SimRemoveField_Click(object sender, RoutedEventArgs e)
        {
            if (SimFieldsGrid.SelectedItem is FieldDefinition selected)
            {
                _simFields.Remove(selected);
            }
            else if (_simFields.Count > 0)
            {
                _simFields.RemoveAt(_simFields.Count - 1);
            }
            UpdatePreview();
        }

        // Push any in-progress cell edit into the bound FieldDefinition before
        // we read the field list to generate or build columns.
        private void CommitFieldEdits()
        {
            try
            {
                SimFieldsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                SimFieldsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch
            {
                // A commit can throw if there is no active edit; nothing to do.
            }
        }

        // ---- Generate / send / stream -------------------------------------

        private void SimPreview_Click(object sender, RoutedEventArgs e) => UpdatePreview();

        private void UpdatePreview()
        {
            if (!_simReady) return;
            CommitFieldEdits();
            try
            {
                // A throwaway generator so previewing never advances the real
                // incrementing counters used for actual sending.
                SimPreviewBox.Text = new MessageGenerator().Generate(_simFormat);
            }
            catch
            {
                // Ignore preview-time formatting errors.
            }
        }

        private async void SimGenerateSend_Click(object sender, RoutedEventArgs e)
            => await GenerateAndSendOnceAsync();

        private async void SimStreamTimer_Tick(object? sender, EventArgs e)
            => await GenerateAndSendOnceAsync();

        private async Task GenerateAndSendOnceAsync()
        {
            if (!_serial.IsOpen) return;

            CommitFieldEdits();

            string message;
            try
            {
                message = _simGenerator.Generate(_simFormat);
            }
            catch (Exception ex)
            {
                StopStreaming();
                MessageBox.Show(this, $"Could not generate a message: {ex.Message}", "Simulator",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SimPreviewBox.Text = message;

            try
            {
                // NMEA/CSV consumers expect line-terminated records.
                await _serial.SendAsync(message + "\r\n");
                _simSentCount++;
                SimSentCountText.Text = $"{_simSentCount} sent";
            }
            catch (Exception ex) when (ex is SerialPortException or InvalidOperationException)
            {
                StopStreaming();
                ReportSendError(ex);
            }
        }

        private void SimStream_Changed(object sender, RoutedEventArgs e)
        {
            if (!_simReady) return;

            if (SimStreamCheck.IsChecked == true && _serial.IsOpen)
            {
                _simStreamTimer!.Interval = TimeSpan.FromMilliseconds(GetIntervalMs());
                _simStreamTimer.Start();
            }
            else
            {
                StopStreaming();
            }
        }

        private void SimInterval_Changed(object sender, TextChangedEventArgs e)
        {
            if (!_simReady) return;
            if (_simStreamTimer is { IsEnabled: true })
            {
                _simStreamTimer.Interval = TimeSpan.FromMilliseconds(GetIntervalMs());
            }
        }

        private void StopStreaming()
        {
            _simStreamTimer?.Stop();
            if (SimStreamCheck.IsChecked == true)
            {
                SimStreamCheck.IsChecked = false; // re-enters here, but the timer is already stopped
            }
        }

        private double GetIntervalMs()
        {
            if (double.TryParse(SimIntervalBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double ms))
            {
                return Math.Max(50, ms);
            }
            return 1000;
        }

        // ---- Parse incoming into a table ----------------------------------

        private void SimParse_Changed(object sender, RoutedEventArgs e)
        {
            if (!_simReady) return;
            _simParseEnabled = SimParseCheck.IsChecked == true;
            if (_simParseEnabled)
            {
                RebuildParsedTable();
            }
        }

        private void SimClearTable_Click(object sender, RoutedEventArgs e)
        {
            _simParsedTable?.Rows.Clear();
            _simRxBuffer.Clear();
        }

        // Builds the table's columns from the current field names (plus a
        // checksum column for NMEA). Column names are made unique/non-empty so
        // the DataTable is happy.
        private void RebuildParsedTable()
        {
            CommitFieldEdits();

            var table = new DataTable();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int index = 1;
            foreach (var field in _simFields)
            {
                string baseName = string.IsNullOrWhiteSpace(field.Name) ? $"field{index}" : field.Name.Trim();
                table.Columns.Add(UniqueColumnName(baseName, used), typeof(string));
                index++;
            }

            if (_simFormat.Kind == MessageFormatKind.Nmea)
            {
                table.Columns.Add(UniqueColumnName("checksum", used), typeof(string));
            }

            _simParsedTable = table;
            _simRxBuffer.Clear();
            SimParsedGrid.ItemsSource = table.DefaultView;
        }

        private static string UniqueColumnName(string baseName, HashSet<string> used)
        {
            string name = baseName;
            int suffix = 2;
            while (!used.Add(name))
            {
                name = $"{baseName}_{suffix++}";
            }
            return name;
        }

        /// <summary>
        /// Called on the UI thread for every received chunk. Accumulates bytes
        /// into lines and parses each complete line into a table row.
        /// </summary>
        private void HandleSimulatorIncoming(string text)
        {
            if (!_simParseEnabled || _simParsedTable is null) return;

            _simRxBuffer.Append(text);

            int newline;
            while ((newline = IndexOfNewline(_simRxBuffer)) >= 0)
            {
                string line = _simRxBuffer.ToString(0, newline).TrimEnd('\r');
                _simRxBuffer.Remove(0, newline + 1);
                if (line.Length > 0)
                {
                    AddParsedRow(line);
                }
            }
        }

        private static int IndexOfNewline(StringBuilder sb)
        {
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '\n') return i;
            }
            return -1;
        }

        private void AddParsedRow(string line)
        {
            var table = _simParsedTable!;
            var parsed = MessageParser.Parse(_simFormat, line);

            int columnCount = table.Columns.Count;
            int checksumColumn = _simFormat.Kind == MessageFormatKind.Nmea ? columnCount - 1 : -1;
            int valueColumns = checksumColumn >= 0 ? columnCount - 1 : columnCount;

            var row = table.NewRow();
            if (parsed.Success)
            {
                for (int i = 0; i < valueColumns && i < parsed.Values.Count; i++)
                {
                    row[i] = parsed.Values[i];
                }
                if (checksumColumn >= 0)
                {
                    row[checksumColumn] = parsed.ChecksumValid switch
                    {
                        true => "✓",   // check mark
                        false => "✗",  // ballot x
                        null => "—",   // em dash (not present)
                    };
                }
            }
            else if (valueColumns > 0)
            {
                row[0] = parsed.Error ?? "parse error";
            }

            table.Rows.Add(row);

            // Keep memory bounded during long streaming sessions.
            const int maxRows = 1000;
            while (table.Rows.Count > maxRows)
            {
                table.Rows.RemoveAt(0);
            }

            if (SimParsedGrid.Items.Count > 0)
            {
                SimParsedGrid.ScrollIntoView(SimParsedGrid.Items[SimParsedGrid.Items.Count - 1]);
            }
        }
    }
}
