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

            if (simulationName == null)
            {
                this.Title = "Inject interrupt to *untitled simulation*";
            }
            else
            {
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

            m_fullyLoaded = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonInject_Click(object sender, RoutedEventArgs e)
        {
            if (m_emulator.Halted)
            {
                this.Close();
                return;
            }

            RVVector l_vector;
            if (radioButtonInterruptTypeReset.IsChecked == true)
            {
                l_vector = RVVector.Reset;
            }
            else
            {
                int l_vectorNumber;
                if (int.TryParse(textBoxVectorNumber.Text, out l_vectorNumber))
                {
                    if (l_vectorNumber == 1 || (l_vectorNumber >= 8 && l_vectorNumber <= 31))
                    {
                        l_vector = (RVVector)l_vectorNumber;
                    }
                    else
                    {
                        String l_message;
                        if (l_vectorNumber < 1 || l_vectorNumber > 31)
                        {
                            l_message = "Invalid vector number.";
                        }
                        else
                        {
                            l_message = "Illegal vector number.";
                        }

                        MessageBox.Show(l_message + "\nValid vector numbers are 1 and 8..31", "Invalid vector", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Could not parse vector number!", "Invalid number", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            ushort l_delay;
            if (radioButtonDeliveryImmediate.IsChecked == true)
            {
                l_delay = 0;
            }
            else
            {
                if ((ushort.TryParse(textBoxDelayCount.Text, out l_delay) == false) || l_delay == 0 || l_delay > 1000)
                {
                    MessageBox.Show("Invalid delay count.\nValid range is 1..1000", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            m_emulator.QueueExternalInterrupt(l_vector, l_delay);
            this.Close();
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
