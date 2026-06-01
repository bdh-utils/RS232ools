using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace RS232ools
{
    /// <summary>
    /// About dialog for the bdh-utils RS232ools utility. Shows the app name and
    /// version, the bdh-utils brand line, a link to the GitHub org, and the
    /// copyright line. Styled with the central bdh-utils brand palette.
    /// </summary>
    public partial class AboutWindow : Window
    {
        private const string OrgUrl = "https://github.com/bdh-utils";

        public AboutWindow()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionText = version is null
                ? string.Empty
                : $"v{version.Major}.{version.Minor}.{version.Build}";

            AppNameText.Text = $"RS232ools {versionText}".TrimEnd();
            CopyrightText.Text = $"© {DateTime.Now.Year} bdh-utils";
        }

        private void OrgLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OrgUrl,
                UseShellExecute = true
            });

            if (e is RequestNavigateEventArgs nav)
            {
                nav.Handled = true;
            }
        }
    }
}
