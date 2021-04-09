using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for InjectInterruptWindow.xaml
    /// </summary>
    public partial class InjectInterruptWindow : Window
    {
        bool m_fullyLoaded = false;

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

            m_fullyLoaded = true;
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
            if (m_fullyLoaded)
            {
                textBoxVectorNumber.IsEnabled = false;
                textBlockInterruptName.Visibility = Visibility.Hidden;
            }
        }

        private void radioButtonInterruptTypeVector_Checked(object sender, RoutedEventArgs e)
        {
            if (m_fullyLoaded)
            {
                textBoxVectorNumber.IsEnabled = true;
                textBlockInterruptName.Visibility = Visibility.Visible;
            }
        }

        private void radioButtonDeliveryImmediate_Checked(object sender, RoutedEventArgs e)
        {
            if (m_fullyLoaded)
            {
                textBoxDelayCount.IsEnabled = false;
            }
        }

        private void radioButtonDeliveryDelayed_Checked(object sender, RoutedEventArgs e)
        {
            if (m_fullyLoaded)
            {
                textBoxDelayCount.IsEnabled = true;
            }
        }

        private void textBoxVectorNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_fullyLoaded == false)
            {
                return;
            }

            int l_vectorNumber;
            if (int.TryParse(textBoxVectorNumber.Text, out l_vectorNumber))
            {
                if (l_vectorNumber <= 0 || l_vectorNumber > 31)
                {
                    textBlockInterruptName.Text = "Invalid interrupt vector";
                    textBlockInterruptName.Foreground = Brushes.Red;
                }
                else if (l_vectorNumber == 1)
                {
                    textBlockInterruptName.Text = "NMI";
                    textBlockInterruptName.Foreground = Brushes.Black;
                }
                else if (l_vectorNumber >= 2 && l_vectorNumber <= 7)
                {
                    textBlockInterruptName.Text = "Illegal interrupt vector";
                    textBlockInterruptName.Foreground = Brushes.Red;
                }
                else
                {
                    textBlockInterruptName.Text = "External" + l_vectorNumber; // will modify later
                    textBlockInterruptName.Foreground = Brushes.Black;
                }
            }
            else
            {
                textBlockInterruptName.Text = "Invalid number";
                textBlockInterruptName.Foreground = Brushes.Red;
            }
        }
    }
}
