using System;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for DataViewEntry.xaml
    /// </summary>
    public partial class DataViewEntry : UserControl
    {
        UInt32 m_address;
        TextBlock[] m_dataTextBlocks;

        public DataViewEntry()
        {
            InitializeComponent();

            m_dataTextBlocks = new TextBlock[8];

            m_dataTextBlocks[0] = textBlockData0;
            m_dataTextBlocks[1] = textBlockData1;
            m_dataTextBlocks[2] = textBlockData2;
            m_dataTextBlocks[3] = textBlockData3;
            m_dataTextBlocks[4] = textBlockData4;
            m_dataTextBlocks[5] = textBlockData5;
            m_dataTextBlocks[6] = textBlockData6;
            m_dataTextBlocks[7] = textBlockData7;
        }

        public void DisplayData(UInt32 baseAddress, byte?[] data)
        {
            if (data.Length != 8)
            {
                throw new Exception("Invalid data length.");
            }

            m_address = baseAddress;
            textBlockBaseAddress.Text = String.Format("{0,8:X8}", baseAddress);

            for (int i_index = 0; i_index < 8; ++i_index)
            {
                if (data[i_index] == null)
                {
                    m_dataTextBlocks[i_index].Text = "";
                }
                else
                {
                    byte l_byte = (byte)data[i_index];
                    m_dataTextBlocks[i_index].Text = String.Format("{0,2:X2}", (UInt32)l_byte);
                }
            }
        }

        public UInt32 GetBaseAddress()
        {
            return m_address;
        }
    }
}
