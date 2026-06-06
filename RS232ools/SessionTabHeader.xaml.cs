using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RS232ools
{
    /// <summary>
    /// The header used for each session tab in <see cref="MainWindow"/>: a
    /// renameable label and a close button. The label auto-tracks the session's
    /// port until the user renames it, after which the chosen name sticks.
    /// </summary>
    public partial class SessionTabHeader : UserControl
    {
        private bool _userRenamed;

        public SessionTabHeader()
        {
            InitializeComponent();
        }

        /// <summary>Raised when the user clicks this tab's close button.</summary>
        public event EventHandler? CloseRequested;

        /// <summary>The text shown on the tab.</summary>
        public string Title
        {
            get => TitleText.Text;
            set => TitleText.Text = value;
        }

        /// <summary>
        /// Sets the label from the session's suggested name (its port), unless the
        /// user has already given the tab a name of their own.
        /// </summary>
        public void SuggestTitle(string suggested)
        {
            if (!_userRenamed && !string.IsNullOrWhiteSpace(suggested))
            {
                TitleText.Text = suggested;
            }
        }

        private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Double-click to start editing; a single click should fall through so
            // the tab still selects normally.
            if (e.ClickCount < 2) return;
            e.Handled = true;
            BeginEdit();
        }

        private void BeginEdit()
        {
            EditBox.Text = TitleText.Text;
            EditBox.Visibility = Visibility.Visible;
            TitleText.Visibility = Visibility.Collapsed;
            EditBox.Focus();
            EditBox.SelectAll();
        }

        private void CommitEdit()
        {
            if (EditBox.Visibility != Visibility.Visible) return;

            string name = EditBox.Text.Trim();
            if (name.Length > 0)
            {
                TitleText.Text = name;
                _userRenamed = true;
            }
            EndEdit();
        }

        private void EndEdit()
        {
            EditBox.Visibility = Visibility.Collapsed;
            TitleText.Visibility = Visibility.Visible;
        }

        private void EditBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                CommitEdit();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                EndEdit(); // discard
            }
        }

        private void EditBox_LostFocus(object sender, RoutedEventArgs e) => CommitEdit();

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
