using System.Windows;
using System.Windows.Controls;

namespace Emu5
{
    public enum Verbosity
    {
        Minimal = 0,
        Normal,
        Detailed
    }

    /// <summary>
    /// Interaction logic for LogSettingsPanel.xaml
    /// </summary>
    public partial class LogSettingsPanel : UserControl
    {
        public LogSettingsPanel()
        {
            InitializeComponent();

            UpdateLogUI();
        }

        public void SetLoggingEnabled(bool enabled)
        {
            checkBoxEnableLogging.IsChecked = enabled;
        }

        public void SetVerbosityLevel(Verbosity level)
        {
            comboBoxVerbosity.SelectedIndex = (int)level;
        }

        public void SetEcallLoggingDisable(bool disable)
        {
            checkBoxDontLogEcall.IsChecked = disable;
        }

        public bool GetLoggingEnabled()
        {
            return checkBoxEnableLogging.IsChecked == null ? false : (bool)checkBoxEnableLogging.IsChecked;
        }

        public Verbosity GetVerbosityLevel()
        {
            return (Verbosity)comboBoxVerbosity.SelectedIndex;
        }

        public bool GetEcallLoggingDisable()
        {
            return checkBoxDontLogEcall.IsChecked == null ? false : (bool)checkBoxDontLogEcall.IsChecked;
        }

        private void checkBoxEnableLogging_CheckChanged(object sender, RoutedEventArgs e)
        {
            UpdateLogUI();
        }

        private void UpdateLogUI()
        {
            bool l_checked = checkBoxEnableLogging.IsChecked == null ? false : (bool)checkBoxEnableLogging.IsChecked;

            comboBoxVerbosity.IsEnabled = l_checked;
            checkBoxDontLogEcall.IsEnabled = l_checked;
        }
    }
}
