using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for InstructionViewEntry.xaml
    /// </summary>
    public partial class InstructionViewEntry : UserControl
    {
        public delegate void ToggleBreakpointDelegate(bool active, UInt32 address);

        bool m_breakpoint;
        UInt32? m_address;
        UInt32 m_previousValue;

        ToggleBreakpointDelegate m_toggleBreakpointCallback;

        public InstructionViewEntry(ToggleBreakpointDelegate handler)
        {
            InitializeComponent();
            m_address = null;

            m_toggleBreakpointCallback = handler;
        }

        public void DisplayData(bool breakpoint, UInt32 address, byte?[] rawData, RVLabelReferenceMap labelMap, Dictionary<UInt32, String> pseudoInstructionMap)
        {
            if (rawData.Length != 4)
            {
                throw new Exception("Invalid data length.");
            }

            bool l_computeValue = false;

            UInt32 l_instructionData = 0x0;
            bool l_validInstructionData = true;

            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
            {
                if (rawData[i_byteIndex] == null)
                {
                    l_validInstructionData = false;
                    break;
                }
                byte l_byte = (byte)rawData[i_byteIndex];
                l_instructionData |= ((UInt32)l_byte) << (8 * i_byteIndex);
            }

            textBlockInstruction.Foreground = Brushes.Black;
            textBlockRawValue.Foreground = new SolidColorBrush(Color.FromRgb(0x9B, 0x9B, 0x9B));

            if (m_address == null || m_address != address)
            {
                l_computeValue = true;
            }
            else if (l_validInstructionData && (m_previousValue != l_instructionData))
            {
                l_computeValue = true;

                textBlockInstruction.Foreground = Brushes.Red;
                textBlockRawValue.Foreground = Brushes.Red;
            }
            m_address = address;
            m_previousValue = l_instructionData;
            
            textBlockAddress.Text = String.Format("{0,8:X8}", address);

            if (l_computeValue) // optimization: dont dissassemble again if data did not change
            {
                String l_annotation = "";
                String[] l_labels = labelMap.FindByAddress(address);
                for (int i_labelIndex = 0; i_labelIndex < l_labels.Length; ++i_labelIndex)
                {
                    if (i_labelIndex != 0)
                    {
                        l_annotation += ", ";
                    }
                    l_annotation += l_labels[i_labelIndex];
                }
                if (String.IsNullOrEmpty(l_annotation) == false)
                {
                    l_annotation += ":";
                }
                String l_pseudoInstruction;
                if (pseudoInstructionMap.TryGetValue(address, out l_pseudoInstruction))
                {
                    if (String.IsNullOrEmpty(l_annotation) == false)
                    {
                        l_annotation += " ";
                    }
                    l_annotation += l_pseudoInstruction;
                }

                textBlockAnnotation.Text = l_annotation;

                if (l_validInstructionData)
                {
                    Tuple<String, String> l_decodedInstruction = RVInstructions.DisassembleIntruction(l_instructionData, labelMap, (UInt32)m_address);

                    textBlockInstruction.Text = l_decodedInstruction.Item1;
                    textBlockRawValue.Text = l_decodedInstruction.Item2;
                }
                else
                {
                    textBlockInstruction.Text = "";
                    textBlockRawValue.Text = "";
                }
            }

            m_breakpoint = breakpoint;
            buttonToggleBreakpoint.Content = m_breakpoint ? new Ellipse { Height = 11, Width = 11, Fill = Brushes.Red, Stroke = Brushes.Red } : null;
        }

        public bool Highlighted
        {
            set
            {
                this.Background = value ? Brushes.Yellow : Brushes.Transparent;
            }
        }

        public UInt32 GetAddress()
        {
            return (UInt32)m_address;
        }

        private void buttonToggleBreakpoint_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m_breakpoint = !m_breakpoint;

            buttonToggleBreakpoint.Content = m_breakpoint ? new Ellipse { Height = 11, Width = 11, Fill = Brushes.Red, Stroke = Brushes.Red } : null;
            m_toggleBreakpointCallback(m_breakpoint, (UInt32)m_address);
        }
    }
}
