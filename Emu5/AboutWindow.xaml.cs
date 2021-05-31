using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            Version l_version = Assembly.GetExecutingAssembly().GetName().Version;
            String l_versionString = String.Format("{0}.{1}.{2}", l_version.Major, l_version.Minor, l_version.Build);

            textBlockVersion.Text = l_versionString;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            String l_startInfo = ((Hyperlink)sender).NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(l_startInfo));
            this.Close();
        }
    }
}
