using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RS232ools.Devices;
using RS232ools.Serial;

namespace RS232ools
{
    /// <summary>
    /// The "Advanced" tab: a rule-based device responder. Each rule matches an
    /// incoming line (a {placeholder} template or a regex), captures named values,
    /// computes derived variables with arithmetic/boolean expressions, and sends a
    /// reply with those values substituted in. Matching/transform logic lives in
    /// the WPF-free <see cref="Devices"/> layer; this partial wires it to the grids
    /// and the shared serial service.
    /// </summary>
    public partial class SessionView
    {
        private readonly ObservableCollection<ResponderRule> _rules = new();
        private readonly StringBuilder _responderRxBuffer = new();
        private bool _responderActive;
        private bool _advReady;

        // ---- Setup --------------------------------------------------------

        private void InitAdvanced()
        {
            AdvReplyEndingCombo.ItemsSource = LineEndings;
            AdvReplyEndingCombo.DisplayMemberPath = nameof(LineEndingOption.Label);
            AdvReplyEndingCombo.SelectedIndex = 3; // CR+LF

            // Starter rules that show capture + transform.
            _rules.Add(new ResponderRule { Name = "ping", Pattern = "PING", Response = "PONG" });

            var scale = new ResponderRule { Name = "scale", Pattern = "READ {raw}", Response = "VAL={scaled}" };
            scale.Variables.Add(new DerivedVariable { Name = "scaled", Expression = "raw * 0.1" });
            _rules.Add(scale);

            // String example: RX "abcde[req]123" -> TX "abcde[res]123".
            var reqres = new ResponderRule { Name = "reqres", Pattern = "abcde[{tag}]123", Response = "abcde[{out}]123" };
            reqres.Variables.Add(new DerivedVariable { Name = "out", Expression = "replace(tag, \"req\", \"res\")" });
            _rules.Add(reqres);

            AdvRulesGrid.ItemsSource = _rules;

            _advReady = true;
            UpdateAdvancedConnectionState(_serial.IsOpen);
            UpdateVariableEditorState();
        }

        /// <summary>Enables auto-respond only while connected; stops it otherwise.</summary>
        private void UpdateAdvancedConnectionState(bool open)
        {
            if (!_advReady) return;
            AdvAutoRespondCheck.IsEnabled = open;
            if (!open)
            {
                AdvAutoRespondCheck.IsChecked = false; // also clears _responderActive via handler
                _responderActive = false;
            }
        }

        private void AdvAutoRespond_Changed(object sender, RoutedEventArgs e)
        {
            if (!_advReady) return;
            CommitRuleEdits();
            _responderActive = AdvAutoRespondCheck.IsChecked == true && _serial.IsOpen;
        }

        // ---- Rule editing -------------------------------------------------

        private void AdvAddRule_Click(object sender, RoutedEventArgs e)
        {
            _rules.Add(new ResponderRule { Name = "rule" + (_rules.Count + 1) });
        }

        private void AdvRemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (AdvRulesGrid.SelectedItem is ResponderRule selected)
            {
                _rules.Remove(selected);
            }
            else if (_rules.Count > 0)
            {
                _rules.RemoveAt(_rules.Count - 1);
            }
        }

        private void AdvRulesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Bind the variables grid to the selected rule's variable list.
            AdvVarsGrid.ItemsSource = (AdvRulesGrid.SelectedItem as ResponderRule)?.Variables;
            UpdateVariableEditorState();
        }

        private void UpdateVariableEditorState()
        {
            bool hasRule = AdvRulesGrid.SelectedItem is ResponderRule;
            AdvAddVarButton.IsEnabled = hasRule;
            AdvRemoveVarButton.IsEnabled = hasRule;
            AdvVarsHeader.Text = hasRule
                ? $"Variables for rule '{((ResponderRule)AdvRulesGrid.SelectedItem).Name}'"
                : "Variables (select a rule)";
        }

        private void AdvAddVar_Click(object sender, RoutedEventArgs e)
        {
            if (AdvRulesGrid.SelectedItem is not ResponderRule rule) return;
            rule.Variables.Add(new DerivedVariable { Name = "var" + (rule.Variables.Count + 1) });
        }

        private void AdvRemoveVar_Click(object sender, RoutedEventArgs e)
        {
            if (AdvRulesGrid.SelectedItem is not ResponderRule rule) return;
            if (AdvVarsGrid.SelectedItem is DerivedVariable selected)
            {
                rule.Variables.Remove(selected);
            }
            else if (rule.Variables.Count > 0)
            {
                rule.Variables.RemoveAt(rule.Variables.Count - 1);
            }
        }

        // Push any in-progress grid edits into the bound objects before matching.
        private void CommitRuleEdits()
        {
            try
            {
                AdvRulesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                AdvRulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                AdvVarsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                AdvVarsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch
            {
                // No active edit; nothing to do.
            }
        }

        // ---- Save / load config -------------------------------------------

        private void AdvSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            CommitRuleEdits();

            var config = new AdvancedConfig
            {
                ReplyLineEnding = ReplyLineEnding(),
                Rules = new List<ResponderRule>(_rules),
            };

            var dialog = new SaveFileDialog
            {
                Title = "Save advanced configuration",
                Filter = "Advanced config (*.json)|*.json|All files (*.*)|*.*",
                FileName = "rs232-advanced-config.json",
                DefaultExt = ".json",
            };
            if (dialog.ShowDialog(OwnerWindow) != true) return;

            try
            {
                File.WriteAllText(dialog.FileName, AdvancedConfigCodec.Serialize(config));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(OwnerWindow, $"Could not save the configuration: {ex.Message}", "Save config",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdvLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Load advanced configuration",
                Filter = "Advanced config (*.json)|*.json|All files (*.*)|*.*",
            };
            if (dialog.ShowDialog(OwnerWindow) != true) return;

            AdvancedConfig config;
            try
            {
                config = AdvancedConfigCodec.Deserialize(File.ReadAllText(dialog.FileName));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                MessageBox.Show(OwnerWindow, $"Could not load the configuration: {ex.Message}", "Load config",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ApplyAdvancedConfig(config);
        }

        private void ApplyAdvancedConfig(AdvancedConfig config)
        {
            // Stop responding while the rule set is swapped out.
            AdvAutoRespondCheck.IsChecked = false;
            _responderActive = false;
            _responderRxBuffer.Clear();

            _rules.Clear();
            foreach (var rule in config.Rules)
            {
                if (rule is not null) _rules.Add(rule);
            }

            // Restore the reply ending by matching its value.
            AdvReplyEndingCombo.SelectedItem =
                LineEndings.FirstOrDefault(o => o.Value == config.ReplyLineEnding) ?? LineEndings[3];

            AdvVarsGrid.ItemsSource = null;
            UpdateVariableEditorState();
        }

        // ---- Incoming -> match -> reply -----------------------------------

        /// <summary>
        /// Called on the UI thread for each received chunk. Accumulates complete
        /// lines and runs each through the rules.
        /// </summary>
        private void HandleResponderIncoming(string text)
        {
            if (!_responderActive) return;

            _responderRxBuffer.Append(text);

            int newline;
            while ((newline = IndexOfNewline(_responderRxBuffer)) >= 0)
            {
                string line = _responderRxBuffer.ToString(0, newline).TrimEnd('\r');
                _responderRxBuffer.Remove(0, newline + 1);
                if (line.Length > 0)
                {
                    ProcessResponderLine(line);
                }
            }
        }

        private async void ProcessResponderLine(string line)
        {
            ResponderResult result = Responder.Respond(_rules, line);

            if (result.Error is not null)
            {
                AdvLog($"RX {line}  !! {result.Error}");
                return;
            }
            if (!result.Matched || result.Response is null)
            {
                return; // unmatched lines are ignored silently
            }

            AdvLog($"RX {line}  ->  [{result.RuleName}]  TX {result.Response}");

            try
            {
                await _serial.SendAsync(result.Response + ReplyLineEnding());
            }
            catch (Exception ex) when (ex is SerialPortException or InvalidOperationException)
            {
                AdvLog($"!! send failed: {ex.Message}");
            }
        }

        private string ReplyLineEnding()
            => AdvReplyEndingCombo.SelectedItem is LineEndingOption opt ? opt.Value : "\r\n";

        // ---- Activity log -------------------------------------------------

        private void AdvClearLog_Click(object sender, RoutedEventArgs e) => AdvLogBox.Clear();

        private void AdvLog(string message)
        {
            const int maxChars = 20000;
            string line = $"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}";

            AdvLogBox.AppendText(line);
            if (AdvLogBox.Text.Length > maxChars)
            {
                AdvLogBox.Text = AdvLogBox.Text.Substring(AdvLogBox.Text.Length - maxChars);
            }
            AdvLogBox.ScrollToEnd();
        }
    }
}
