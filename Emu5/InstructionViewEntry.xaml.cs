using System;
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
        UInt32 m_address;

        public InstructionViewEntry()
        {
            InitializeComponent();
        }

        public void DisplayData(bool breakpoint, UInt32 address, byte?[] rawData, String annotation)
        {
            if (rawData.Length != 4)
            {
                throw new Exception("Invalid data length.");
            }

            m_address = address;

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
                l_instructionData <<= 8;
                l_instructionData |= (UInt32)l_byte;
            }

            // call decode instruction

            textBlockAddress.Text = String.Format("{0,8:X8}", address);
            textBlockInstruction.Text = "";
            textBlockRawValue.Text = l_validInstructionData ? String.Format("0x{0,8:X8}", l_instructionData) : "";
            textBlockAnnotation.Text = annotation;

            buttonToggleBreakpoint.Content = breakpoint ? new Ellipse { Height = 11, Width = 11 } : null;
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
            return m_address;
        }
    }
}
