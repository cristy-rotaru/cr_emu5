using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for IOPanelWindow.xaml
    /// </summary>
    public partial class IOPanelWindow : Window
    {
        IOPanel m_ioPanel;

        Rectangle[] m_segments = new Rectangle[16];
        Button[] m_buttons = new Button[16];
        Ellipse[] m_leds = new Ellipse[16];
        Slider[] m_switches = new Slider[16];

        bool m_threadShutdown;
        bool m_disposeWhenClosing = false;

        public IOPanelWindow(IOPanel peripheral)
        {
            InitializeComponent();

            m_ioPanel = peripheral;

            m_threadShutdown = false;

            m_segments[0] = rectangle0A;
            m_segments[1] = rectangle0B;
            m_segments[2] = rectangle0C;
            m_segments[3] = rectangle0D;
            m_segments[4] = rectangle0E;
            m_segments[5] = rectangle0F;
            m_segments[6] = rectangle0G;
            m_segments[7] = null;
            m_segments[8] = rectangle1A;
            m_segments[9] = rectangle1B;
            m_segments[10] = rectangle1C;
            m_segments[11] = rectangle1D;
            m_segments[12] = rectangle1E;
            m_segments[13] = rectangle1F;
            m_segments[14] = rectangle1G;
            m_segments[15] = null;

            m_buttons[0] = buttonB0;
            m_buttons[1] = buttonB1;
            m_buttons[2] = buttonB2;
            m_buttons[3] = buttonB3;
            m_buttons[4] = buttonB4;
            m_buttons[5] = buttonB5;
            m_buttons[6] = buttonB6;
            m_buttons[7] = buttonB7;
            m_buttons[8] = buttonB8;
            m_buttons[9] = buttonB9;
            m_buttons[10] = buttonB10;
            m_buttons[11] = buttonB11;
            m_buttons[12] = buttonB12;
            m_buttons[13] = buttonB13;
            m_buttons[14] = buttonB14;
            m_buttons[15] = buttonB15;

            m_leds[0] = ellipseLED0;
            m_leds[1] = ellipseLED1;
            m_leds[2] = ellipseLED2;
            m_leds[3] = ellipseLED3;
            m_leds[4] = ellipseLED4;
            m_leds[5] = ellipseLED5;
            m_leds[6] = ellipseLED6;
            m_leds[7] = ellipseLED7;
            m_leds[8] = ellipseLED8;
            m_leds[9] = ellipseLED9;
            m_leds[10] = ellipseLED10;
            m_leds[11] = ellipseLED11;
            m_leds[12] = ellipseLED12;
            m_leds[13] = ellipseLED13;
            m_leds[14] = ellipseLED14;
            m_leds[15] = ellipseLED15;

            m_switches[0] = sliderSW0;
            m_switches[1] = sliderSW1;
            m_switches[2] = sliderSW2;
            m_switches[3] = sliderSW3;
            m_switches[4] = sliderSW4;
            m_switches[5] = sliderSW5;
            m_switches[6] = sliderSW6;
            m_switches[7] = sliderSW7;
            m_switches[8] = sliderSW8;
            m_switches[9] = sliderSW9;
            m_switches[10] = sliderSW10;
            m_switches[11] = sliderSW11;
            m_switches[12] = sliderSW12;
            m_switches[13] = sliderSW13;
            m_switches[14] = sliderSW14;
            m_switches[15] = sliderSW15;

            ThreadStart l_threadFunction = new ThreadStart(
            () => {
                UInt16 l_oldSegments = m_ioPanel.GetSegmentValues();
                Dispatcher.BeginInvoke(new Action(() => UpdateSegmentDisplay(l_oldSegments)));

                UInt16 l_oldLeds = m_ioPanel.GetLEDValues();
                Dispatcher.BeginInvoke(new Action(() => UpdateLEDStatus(l_oldLeds)));

                while (m_threadShutdown == false)
                {
                    UInt16 l_segments = m_ioPanel.GetSegmentValues();
                    if (l_segments != l_oldSegments)
                    {
                        l_oldSegments = l_segments;
                        Dispatcher.Invoke(new Action(() => UpdateSegmentDisplay(l_segments)));
                    }

                    UInt16 l_leds = m_ioPanel.GetLEDValues();
                    if (l_leds != l_oldLeds)
                    {
                        l_oldLeds = l_leds;
                        Dispatcher.Invoke(new Action(() => UpdateLEDStatus(l_leds)));
                    }
                }
            });

            Thread l_monitorThread = new Thread(l_threadFunction);
            l_monitorThread.Start();
        }

        private void IOPanelWindow_Closing(object sender, CancelEventArgs e)
        {
            if (m_disposeWhenClosing == false)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void buttonB_Click(object sender, RoutedEventArgs e)
        {
            for (int i_buttonIndex = 0; i_buttonIndex < 16; ++i_buttonIndex)
            {
                if (m_buttons[i_buttonIndex] == sender)
                {
                    m_ioPanel.RegisterButtonPressed(i_buttonIndex);

                    break;
                }
            }
        }

        private void sliderSW_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            for (int i_switchIndex = 0; i_switchIndex < 16; ++i_switchIndex)
            {
                if (m_switches[i_switchIndex] == sender)
                {
                    m_ioPanel.SwitchStateChanged(i_switchIndex, m_switches[i_switchIndex].Value > .5);

                    break;
                }
            }
        }

        private void UpdateSegmentDisplay(UInt16 segments)
        {
            for (int i_segmentIndex = 0; i_segmentIndex < 16; ++i_segmentIndex)
            {
                if (m_segments[i_segmentIndex] != null)
                {
                    m_segments[i_segmentIndex].Fill = (segments & (1 << i_segmentIndex)) != 0 ? Brushes.Red : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0x00, 0x00));
                    m_segments[i_segmentIndex].UpdateLayout();
                }
            }
        }

        private void UpdateLEDStatus(UInt16 leds)
        {
            for (int i_ledIndex = 0; i_ledIndex < 16; ++i_ledIndex)
            {
                m_leds[i_ledIndex].Fill = (leds & (1 << i_ledIndex)) != 0 ? Brushes.Red : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0x00, 0x00));
                m_leds[i_ledIndex].UpdateLayout();
            }
        }

        public void Close(bool dispose)
        {
            m_disposeWhenClosing = dispose;
            this.Close();
        }

        private void IOPanelWindow_Closed(object sender, EventArgs e)
        {
            m_threadShutdown = true;
        }
    }
}
