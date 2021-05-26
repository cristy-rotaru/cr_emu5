using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for PeripheralsSettingsPanel.xaml
    /// </summary>
    public partial class PeripheralsSettingsPanel : UserControl
    {
        public PeripheralsSettingsPanel()
        {
            InitializeComponent();
        }

        public void SetIOPanelEnabled(bool enabled)
        {
            checkBoxIOPanel.IsChecked = enabled;
        }

        public void SetTerminalEnabled(bool enabled)
        {
            checkBoxTerminal.IsChecked = enabled;
        }

        public void SetInterruptInjectorEnabled(bool enabled)
        {
            checkBoxInterruptInjector.IsChecked = enabled;
        }

        public bool GetIOPanelEnabled()
        {
            return checkBoxIOPanel.IsChecked == null ? false : (bool)checkBoxIOPanel.IsChecked;
        }

        public bool GetTerminalEnabled()
        {
            return checkBoxTerminal.IsChecked == null ? false : (bool)checkBoxTerminal.IsChecked;
        }

        public bool GetInterruptInjectorEnabled()
        {
            return checkBoxInterruptInjector.IsChecked == null ? false : (bool)checkBoxInterruptInjector.IsChecked;
        }
    }
}
