using System;
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
        RVEmulator m_emulator;

        Rectangle[] m_segments = new Rectangle[16];
        Button[] m_buttons = new Button[16];
        Ellipse[] m_leds = new Ellipse[16];
        Slider[] m_switches = new Slider[16];

        UInt16 m_segmentValues;
        UInt16 m_buttonEvents;
        UInt16 m_buttonInterruptMask;
        UInt16 m_ledValues;

        public IOPanelWindow(RVEmulator emulator)
        {
            InitializeComponent();

            m_emulator = emulator;

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

            m_segmentValues = 0x0000;
            m_buttonEvents = 0x0000;
            m_buttonInterruptMask = 0x0000;
            m_ledValues = 0x0000;
        }

        public byte[] ReadRegisters(UInt32 offset, int count)
        {
            byte[] l_result = new byte[count];
            bool l_buttonEventsRead = false;

            for (int i_byteIndex = 0; i_byteIndex < count; ++i_byteIndex)
            {
                int l_registerOffset = (int)offset + i_byteIndex;

                switch (l_registerOffset)
                {
                    case 0: // segments LO
                    {
                        lock (m_segments)
                        {
                            l_result[i_byteIndex] = (byte)m_segmentValues;
                        }
                    }
                    break;

                    case 1: // segments HI
                    {
                        lock (m_segments)
                        {
                            l_result[i_byteIndex] = (byte)(m_segmentValues >> 8);
                        }
                    }
                    break;

                    case 2: // buttons LO
                    {
                        lock (m_buttons)
                        {
                            l_result[i_byteIndex] = (byte)m_buttonEvents;
                            l_buttonEventsRead = true;
                        }
                    }
                    break;

                    case 3: // buttons HI
                    {
                        lock (m_buttons)
                        {
                            l_result[i_byteIndex] = (byte)(m_buttonEvents >> 8);
                            l_buttonEventsRead = true;
                        }
                    }
                    break;

                    case 4: // LEDS LO
                    {
                        lock (m_leds)
                        {
                            l_result[i_byteIndex] = (byte)m_ledValues;
                        }
                    }
                    break;

                    case 5: // LEDS HI
                    {
                        lock (m_leds)
                        {
                            l_result[i_byteIndex] = (byte)(m_ledValues >> 8);
                        }
                    }
                    break;

                    case 6: // switches LO
                    {
                        lock (m_switches)
                        {
                            byte l_value = 0;
                            for (int i_switchIndex = 0; i_switchIndex < 8; ++i_switchIndex)
                            {
                                if (m_switches[i_switchIndex].Value > .5)
                                {
                                    l_value |= (byte)(1 << i_switchIndex);
                                }
                            }

                            l_result[i_byteIndex] = l_value;
                        }
                    }
                    break;

                    case 7: // switches HI
                    {
                        lock (m_switches)
                        {
                            byte l_value = 0;
                            for (int i_switchIndex = 0; i_switchIndex < 8; ++i_switchIndex)
                            {
                                if (m_switches[i_switchIndex + 8].Value > .5)
                                {
                                    l_value |= (byte)(1 << i_switchIndex);
                                }
                            }

                            l_result[i_byteIndex] = l_value;
                        }
                    }
                    break;

                    default:
                    {
                        throw new Exception("Invalid register offset");
                    }
                }
            }

            if (l_buttonEventsRead)
            {
                m_buttonEvents = 0x0000;
            }

            return l_result;
        }

        public void WriteRegisters(UInt32 offset, byte[] data)
        {
            bool l_segmentsWritten = false;
            bool l_ledsWritten = false;

            for (int i_byteIndex = 0; i_byteIndex < data.Length; ++i_byteIndex)
            {
                int l_registerOffset = (int)offset + i_byteIndex;

                switch (l_registerOffset)
                {
                    case 0: // segments LO
                    {
                        lock (m_segments)
                        {
                            m_segmentValues &= 0xFF00;
                            m_segmentValues |= (UInt16)(data[i_byteIndex] & 0x7F);
                            l_segmentsWritten = true;
                        }
                    }
                    break;

                    case 1: // segments HI
                    {
                        lock (m_segments)
                        {
                            m_segmentValues &= 0x00FF;
                            m_segmentValues |= (UInt16)(((UInt16)(data[i_byteIndex] & 0x7F)) << 8);
                            l_segmentsWritten = true;
                        }
                    }
                    break;

                    case 2: // buttons LO
                    {
                        lock (m_buttons)
                        {
                            m_buttonInterruptMask &= 0xFF00;
                            m_buttonInterruptMask |= (UInt16)(data[i_byteIndex] & 0x7F);
                        }
                    }
                    break;

                    case 3: // buttons HI
                    {
                        lock (m_buttons)
                        {
                            m_buttonInterruptMask &= 0x00FF;
                            m_buttonInterruptMask |= (UInt16)(((UInt16)(data[i_byteIndex] & 0x7F)) << 8);
                        }
                    }
                    break;

                    case 4: // LEDS LO
                    {
                        lock (m_leds)
                        {
                            m_ledValues &= 0xFF00;
                            m_ledValues |= (UInt16)(data[i_byteIndex] & 0x7F);
                            l_ledsWritten = true;
                        }
                    }
                    break;

                    case 5: // LEDS HI
                    {
                        lock (m_leds)
                        {
                            m_ledValues &= 0x00FF;
                            m_ledValues |= (UInt16)(((UInt16)(data[i_byteIndex] & 0x7F)) << 8);
                            l_ledsWritten = true;
                        }
                    }
                    break;

                    case 6: // switches LO
                    case 7: // switches HI
                        break; // ignore writes to theese registers

                    default:
                    {
                        throw new Exception("Invalid register offset");
                    }
                }
            }

            if (l_segmentsWritten)
            {
                //Dispatcher.BeginInvoke(new Action(UpdateSegmentDisplay));
                UpdateSegmentDisplay();
            }

            if (l_ledsWritten)
            {
                //Dispatcher.BeginInvoke(new Action(UpdateLEDStatus));
                UpdateLEDStatus();
            }
        }

        private void buttonB_Click(object sender, RoutedEventArgs e)
        {
            lock (m_buttons)
            {
                for (int i_buttonIndex = 0; i_buttonIndex < 16; ++i_buttonIndex)
                {
                    if (m_buttons[i_buttonIndex] == sender)
                    {
                        UInt16 l_bit = (UInt16)(1 << i_buttonIndex);
                        m_buttonEvents |= l_bit;

                        if ((m_buttonInterruptMask & l_bit) != 0)
                        {
                            m_emulator.QueueExternalInterrupt(RVVector.External9, 0);
                        }

                        break;
                    }
                }
            }
        }

        private void UpdateSegmentDisplay()
        {
            for (int i_segmentIndex = 0; i_segmentIndex < 16; ++i_segmentIndex)
            {
                lock (m_segments)
                {
                    if (m_segments[i_segmentIndex] != null)
                    {
                        m_segments[i_segmentIndex].Fill = (m_segmentValues & (1 << i_segmentIndex)) != 0 ? Brushes.Red : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0x00, 0x00));
                        m_segments[i_segmentIndex].UpdateLayout();
                    }
                }
            }
        }

        private void UpdateLEDStatus()
        {
            for (int i_ledIndex = 0; i_ledIndex < 16; ++i_ledIndex)
            {
                lock (m_leds)
                {
                    m_leds[i_ledIndex].Fill = (m_ledValues & (1 << i_ledIndex)) != 0 ? Brushes.Red : new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0x00, 0x00));
                    m_leds[i_ledIndex].UpdateLayout();
                }
            }
        }
    }
}
