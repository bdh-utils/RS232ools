using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using RS232ools.Terminal;

namespace RS232ools
{
    /// <summary>
    /// Terminal-tab display logic. Two modes:
    /// <list type="bullet">
    /// <item><b>Received only</b> (default) — the original behaviour: only
    /// incoming data is shown, in plain text.</item>
    /// <item><b>Monitor (sent + received)</b> — echoes everything written to the
    /// port (typed text and streamed file content) interleaved with everything
    /// read from it, each line tagged and coloured by direction so you can watch
    /// the full conversation, including file transfers, as it happens.</item>
    /// </list>
    /// The line/direction prefixing is handled by the testable
    /// <see cref="MonitorFormatter"/>; this partial only renders its segments and
    /// wires up the mode selector.
    /// </summary>
    public partial class SessionView
    {
        private enum TerminalDisplayMode { ReceivedOnly, Monitor }

        private TerminalDisplayMode _displayMode = TerminalDisplayMode.ReceivedOnly;
        private readonly MonitorFormatter _monitor = new();
        private Paragraph? _terminalParagraph;

        // Keep the document bounded over long sessions.
        private const int MaxTerminalInlines = 5000;

        // Cap on how much of a sent file's content is echoed into the view.
        private const int MaxEchoedFileBytes = 64 * 1024;

        private void InitTerminalDisplay()
        {
            _terminalParagraph = new Paragraph { Margin = new Thickness(0) };
            TerminalBox.Document = new FlowDocument(_terminalParagraph)
            {
                Background = Brushes.Transparent,
                PagePadding = new Thickness(4, 2, 4, 2),
            };

            DisplayModeCombo.ItemsSource = new[] { "Received only", "Monitor (sent + received)" };
            DisplayModeCombo.SelectedIndex = 0;
        }

        private void DisplayModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _displayMode = DisplayModeCombo.SelectedIndex == 1
                ? TerminalDisplayMode.Monitor
                : TerminalDisplayMode.ReceivedOnly;
        }

        // ---- Append paths -------------------------------------------------

        /// <summary>Incoming data — always displayed.</summary>
        private void TerminalAppendReceived(string text)
        {
            if (_displayMode == TerminalDisplayMode.Monitor)
            {
                RenderSegments(text, isTx: false);
            }
            else
            {
                AppendRun(text.Replace("\r", string.Empty), BrandTextBrush);
            }
            AfterAppend();
        }

        /// <summary>Outgoing data — only echoed in monitor mode.</summary>
        private void TerminalAppendSent(string text)
        {
            if (_displayMode != TerminalDisplayMode.Monitor) return;
            RenderSegments(text, isTx: true);
            AfterAppend();
        }

        /// <summary>Echoes a sent file's content (capped) when monitoring.</summary>
        private void EchoSentFile(string path, byte[] data)
        {
            if (_displayMode != TerminalDisplayMode.Monitor) return;

            string name = Path.GetFileName(path);
            TerminalAppendSent($"[file: {name}, {data.Length} bytes]\n");

            int shown = Math.Min(data.Length, MaxEchoedFileBytes);
            TerminalAppendSent(Encoding.ASCII.GetString(data, 0, shown) + "\n");

            if (data.Length > shown)
            {
                TerminalAppendSent("[display truncated]\n");
            }
        }

        private void RenderSegments(string text, bool isTx)
        {
            foreach (var segment in _monitor.Append(text, isTx))
            {
                AppendRun(segment.Text, segment.IsTx ? BrandAccentBrush : BrandTextBrush);
            }
        }

        // Adds coloured text, turning "\n" into real FlowDocument line breaks
        // (a Run's embedded newlines are not rendered as breaks otherwise).
        private void AppendRun(string text, Brush brush)
        {
            if (text.Length == 0) return;

            string[] parts = text.Split('\n');
            for (int k = 0; k < parts.Length; k++)
            {
                if (k > 0) _terminalParagraph!.Inlines.Add(new LineBreak());
                if (parts[k].Length > 0)
                {
                    _terminalParagraph!.Inlines.Add(new Run(parts[k]) { Foreground = brush });
                }
            }
        }

        private void AfterAppend()
        {
            var inlines = _terminalParagraph!.Inlines;
            while (inlines.Count > MaxTerminalInlines && inlines.FirstInline is not null)
            {
                inlines.Remove(inlines.FirstInline);
            }

            if (AutoScrollCheck.IsChecked == true)
            {
                TerminalBox.ScrollToEnd();
            }
        }

        private void ClearTerminal()
        {
            _terminalParagraph!.Inlines.Clear();
            _monitor.Reset();
        }

        private Brush BrandTextBrush => (Brush)FindResource("BrandText");
        private Brush BrandAccentBrush => (Brush)FindResource("BrandAccent");
    }
}
