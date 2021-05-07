using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for StackViewEntry.xaml
    /// </summary>
    public partial class StackViewEntry : UserControl
    {
        UInt32? m_address;
        byte?[] m_previousValue;

        TextBlock[] m_halfTextBlocks;
        TextBlock[] m_byteTextBlocks;

        public StackViewEntry()
        {
            InitializeComponent();

            m_address = null;
            m_previousValue = new byte?[4];

            m_halfTextBlocks = new TextBlock[2];
            m_byteTextBlocks = new TextBlock[4];

            m_halfTextBlocks[0] = textBlockHalf0;
            m_halfTextBlocks[1] = textBlockHalf1;

            m_byteTextBlocks[0] = textBlockByte0;
            m_byteTextBlocks[1] = textBlockByte1;
            m_byteTextBlocks[2] = textBlockByte2;
            m_byteTextBlocks[3] = textBlockByte3;
        }

        public void DisplayData(UInt32 address, byte?[] data, bool top)
        {
            if (data.Length != 4)
            {
                throw new Exception("Invalid data length.");
            }

            bool l_highlightChanges = (m_address != null && m_address == address);

            String l_addressString = top ? "SP: " : "    ";
            l_addressString += String.Format("0x{0,8:X8}", address);
            l_addressString += top ? " (TOP) ->" : "       ->";

            textBlockAddress.Text = l_addressString;

            UInt32? l_word = 0x0;
            UInt16?[] l_halves = new UInt16?[2] { 0x0, 0x0 };
            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
            {
                if (data[i_byteIndex] == null)
                {
                    l_word = null;
                    l_halves[i_byteIndex >> 1] = null;

                    m_byteTextBlocks[i_byteIndex].Text = "";
                }
                else
                {
                    if (l_word != null)
                    {
                        l_word |= (UInt32)(data[i_byteIndex]) << (8 * i_byteIndex);
                    }

                    if (l_halves[i_byteIndex >> 1] != null)
                    {
                        l_halves[i_byteIndex >> 1] |= (UInt16)((UInt16)(data[i_byteIndex]) << (8 * (i_byteIndex & 1)));
                    }

                    m_byteTextBlocks[i_byteIndex].Text = String.Format("0x{0,2:X2}", data[i_byteIndex]);
                    m_byteTextBlocks[i_byteIndex].Foreground = (l_highlightChanges && (m_previousValue[i_byteIndex] != data[i_byteIndex])) ? Brushes.Red : Brushes.Black;
                }
            }

            UInt16?[] l_previousHalves = new UInt16?[2];
            for (int i_halfIndex = 0; i_halfIndex < 2; ++i_halfIndex)
            {
                if (m_previousValue[i_halfIndex << 1] != null && m_previousValue[(i_halfIndex << 1) + 1] != null)
                {
                    l_previousHalves[i_halfIndex] = (UInt16)(((UInt16)m_previousValue[(i_halfIndex << 1) + 1] << 8) | m_previousValue[i_halfIndex << 1]);
                }
                else
                {
                    l_previousHalves[i_halfIndex] = null;
                }
            }

            for (int i_halfIndex = 0; i_halfIndex < 2; ++i_halfIndex)
            {
                m_halfTextBlocks[i_halfIndex].Text = (l_halves[i_halfIndex] == null) ? "" : String.Format("0x{0,4:X4}", l_halves[i_halfIndex]);
                m_halfTextBlocks[i_halfIndex].Foreground = (l_highlightChanges && (l_previousHalves[i_halfIndex] != l_halves[i_halfIndex])) ? Brushes.Red : Brushes.Black;
            }

            UInt32? l_previousWord;
            if (l_previousHalves[0] != null && l_previousHalves[1] != null)
            {
                l_previousWord = (UInt32)l_previousHalves[0] | ((UInt32)l_previousHalves[1] << 16);
            }
            else
            {
                l_previousWord = null;
            }

            textBlockWord.Text = (l_word == null) ? "" : String.Format("0x{0,8:X8}", l_word);
            textBlockWord.Foreground = (l_highlightChanges && (l_previousWord != l_word)) ? Brushes.Red : Brushes.Black;

            m_address = address;
            data.CopyTo(m_previousValue, 0);
        }
    }
}
