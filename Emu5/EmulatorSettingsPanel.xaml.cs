using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for EmulatorSettingsPanel.xaml
    /// </summary>
    public partial class EmulatorSettingsPanel : UserControl
    {
        public EmulatorSettingsPanel()
        {
            InitializeComponent();

            UpdateEcallUIElements();
        }

        public void SetClearMemoryMap(bool clear)
        {
            checkBoxClearMemoryMapOnNewSimulation.IsChecked = clear;
        }

        public void SetUseIntegratedEcall(bool use)
        {
            checkBoxUseIntegratedEcallHandler.IsChecked = use;
        }

        public void SetEcallBase(UInt32 address)
        {
            textBoxEcallBase.Text = String.Format("{0,8:X8}", address);
        }

        public bool GetClearMemoryMap()
        {
            return checkBoxClearMemoryMapOnNewSimulation.IsChecked == null ? false : (bool)checkBoxClearMemoryMapOnNewSimulation.IsChecked;
        }

        public bool GetUseIntegratedEcall()
        {
            return checkBoxUseIntegratedEcallHandler.IsChecked == null ? false : (bool)checkBoxUseIntegratedEcallHandler.IsChecked;
        }

        public UInt32? GetEcallBase()
        {
            try
            {
                UInt32 l_address = UInt32.Parse(textBoxEcallBase.Text, NumberStyles.HexNumber);
                return l_address;
            }
            catch
            {
                return null;
            }
        }

        private void checkBoxUseIntegratedEcallHandler_CheckChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateEcallUIElements();
        }

        private void UpdateEcallUIElements()
        {
            bool l_checked = checkBoxUseIntegratedEcallHandler.IsChecked == null ? false : (bool)checkBoxUseIntegratedEcallHandler.IsChecked;

            textBlockEcallBase.Foreground = l_checked ? Brushes.Black : Brushes.Gray;
            textBoxEcallBase.IsEnabled = l_checked;
        }
    }
}
