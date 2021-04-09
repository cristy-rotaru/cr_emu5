using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for InjectInterruptWindow.xaml
    /// </summary>
    public partial class InjectInterruptWindow : Window
    {
        RVEmulator m_emulator;

        public InjectInterruptWindow(String simulationName, RVEmulator emulatorInstance)
        {
            InitializeComponent();

            m_emulator = emulatorInstance;

            String l_nameWithoutExtension = simulationName;
            if (l_nameWithoutExtension.Contains("."))
            {
                int l_lastPointIndex = -1;
                for (int i_charIndex = l_nameWithoutExtension.Length - 1; i_charIndex >= 0; --i_charIndex)
                {
                    if (l_nameWithoutExtension[i_charIndex] == '.')
                    {
                        l_lastPointIndex = i_charIndex;
                        break;
                    }
                }
                if (l_lastPointIndex >= 0)
                {
                    l_nameWithoutExtension = l_nameWithoutExtension.Substring(0, l_lastPointIndex);
                }
            }
            this.Title = "Inject interrupt to " + l_nameWithoutExtension;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonInject_Click(object sender, RoutedEventArgs e)
        {

        }

        private void radioButtonInterruptTypeReset_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void radioButtonInterruptTypeVector_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void radioButtonDeliveryImmediate_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void radioButtonDeliveryDelayed_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void textBoxVectorNumber_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void textBoxDelayCount_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
