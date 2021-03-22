using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for EmulatorPerspective.xaml
    /// </summary>
    public partial class EmulatorPerspective : UserControl
    {
        RVEmulator m_emulator = null;

        List<InstructionViewEntry> m_instructionEntries;
        List<DataViewEntry> m_dataEntries;

        UInt32 m_currentPC;
        UInt32[] m_previousRegisterValues;
        TextBlock[] m_registerTextBoxes;

        public EmulatorPerspective()
        {
            InitializeComponent();

            m_instructionEntries = new List<InstructionViewEntry>();
            m_dataEntries = new List<DataViewEntry>();

            m_currentPC = 0x0;
            m_previousRegisterValues = new UInt32[32];
            m_registerTextBoxes = new TextBlock[32];

            for (int i_index = 0; i_index < 32; ++i_index)
            {
                m_previousRegisterValues[i_index] = 0x0;
            }

            m_registerTextBoxes[0] = textBlockRegisterX0;
            m_registerTextBoxes[1] = textBlockRegisterX1;
            m_registerTextBoxes[2] = textBlockRegisterX2;
            m_registerTextBoxes[3] = textBlockRegisterX3;
            m_registerTextBoxes[4] = textBlockRegisterX4;
            m_registerTextBoxes[5] = textBlockRegisterX5;
            m_registerTextBoxes[6] = textBlockRegisterX6;
            m_registerTextBoxes[7] = textBlockRegisterX7;
            m_registerTextBoxes[8] = textBlockRegisterX8;
            m_registerTextBoxes[9] = textBlockRegisterX9;
            m_registerTextBoxes[10] = textBlockRegisterX10;
            m_registerTextBoxes[11] = textBlockRegisterX11;
            m_registerTextBoxes[12] = textBlockRegisterX12;
            m_registerTextBoxes[13] = textBlockRegisterX13;
            m_registerTextBoxes[14] = textBlockRegisterX14;
            m_registerTextBoxes[15] = textBlockRegisterX15;
            m_registerTextBoxes[16] = textBlockRegisterX16;
            m_registerTextBoxes[17] = textBlockRegisterX17;
            m_registerTextBoxes[18] = textBlockRegisterX18;
            m_registerTextBoxes[19] = textBlockRegisterX19;
            m_registerTextBoxes[20] = textBlockRegisterX20;
            m_registerTextBoxes[21] = textBlockRegisterX21;
            m_registerTextBoxes[22] = textBlockRegisterX22;
            m_registerTextBoxes[23] = textBlockRegisterX23;
            m_registerTextBoxes[24] = textBlockRegisterX24;
            m_registerTextBoxes[25] = textBlockRegisterX25;
            m_registerTextBoxes[26] = textBlockRegisterX26;
            m_registerTextBoxes[27] = textBlockRegisterX27;
            m_registerTextBoxes[28] = textBlockRegisterX28;
            m_registerTextBoxes[29] = textBlockRegisterX29;
            m_registerTextBoxes[30] = textBlockRegisterX30;
            m_registerTextBoxes[31] = textBlockRegisterX31;

            UpdateInfo();
        }

        public void BindEmulator(RVEmulator emulator)
        {
            m_emulator = emulator;
        }

        public void UpdateInfo()
        {
            if (m_emulator == null)
            {
                for (int i_index = 0; i_index < 32; ++i_index)
                {
                    m_previousRegisterValues[i_index] = 0x0;

                    m_registerTextBoxes[i_index].Text = "";
                    m_registerTextBoxes[i_index].Foreground = Brushes.Black;
                }

                textBlockRegisterPC.Text = "";

                m_instructionEntries.Clear();
                m_dataEntries.Clear();

                stackPanelInstructionView.Children.Clear();
                stackPanelMemoryView.Children.Clear();
            }
            else
            {
                UInt32[] l_registerValues = m_emulator.GetRegisterFile();

                for (int i_index = 0; i_index < 32; ++i_index)
                {
                    m_registerTextBoxes[i_index].Foreground = m_previousRegisterValues[i_index] == l_registerValues[i_index] ? Brushes.Black : Brushes.Red;
                    m_registerTextBoxes[i_index].Text = String.Format("0x{0,8:X8}", l_registerValues[i_index]);
                    m_previousRegisterValues[i_index] = l_registerValues[i_index];
                }

                m_currentPC = m_emulator.GetProgramCounter();
                textBlockRegisterPC.Text = String.Format("0x{0,8:X8}", m_currentPC);

                if (m_instructionEntries.Count == 0)
                {
                    RefreshInstructionView();
                }
                else
                {
                    UpdateInstructionView();
                }

                if (m_dataEntries.Count == 0)
                {
                    RefreshDataView();
                }
                else
                {
                    UpdateDataView();
                }
            }
        }

        private void RefreshInstructionView()
        {
            stackPanelInstructionView.Children.Clear();
            m_instructionEntries.Clear();

            int l_entryCount = (int)(stackPanelInstructionView.ActualHeight / 24);
            for (int i_index = 0; i_index < l_entryCount; ++i_index)
            {
                InstructionViewEntry l_viewEntry = new InstructionViewEntry();

                UInt32 l_address = m_currentPC + (UInt32)(i_index << 2);
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().Read(l_address, 4);
                UInt32 l_data = 0x0;
                bool l_validInstructionData = true;

                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                {
                    if (l_rawData[i_byteIndex] == null)
                    {
                        l_validInstructionData = false;
                        break;
                    }
                    byte l_byte = (byte)l_rawData[i_byteIndex];
                    l_data <<= 8;
                    l_data |= (UInt32)l_byte;
                }

                // call decode instruction

                String l_addressString = String.Format("{0,8:X8}", l_address);
                String l_instructionString = "";
                String l_dataString = l_validInstructionData ? String.Format("0x{0,8:X8}", l_data) : "";
                String l_annotationString = "";

                l_viewEntry.DisplayData(false, l_addressString, l_instructionString, l_dataString, l_annotationString);

                l_viewEntry.Highlighted = i_index == 0;

                stackPanelInstructionView.Children.Add(l_viewEntry);
                m_instructionEntries.Add(l_viewEntry);
            }
        }

        private void RefreshDataView()
        {
            stackPanelMemoryView.Children.Clear();
            m_dataEntries.Clear();

            int l_entryCount = (int)(stackPanelMemoryView.ActualHeight / 24);
            for (int i_index = 0; i_index < l_entryCount; ++i_index)
            {
                DataViewEntry l_viewEntry = new DataViewEntry();

                UInt32 l_baseAddress = (UInt32)(i_index << 3);
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().Read(l_baseAddress, 8);
                String[] l_dataString = new String[8];

                for (int i_byteIndex = 0; i_byteIndex < 8; ++i_byteIndex)
                {
                    if (l_rawData[i_byteIndex] == null)
                    {
                        l_dataString[i_byteIndex] = "??";
                    }
                    else
                    {
                        byte l_byte = (byte)l_rawData[i_byteIndex];
                        l_dataString[i_byteIndex] = String.Format("{0,2:X2}", (UInt32)l_byte);
                    }
                }

                String l_baseAddressString = String.Format("{0,8:X8}", l_baseAddress);

                l_viewEntry.DisplayData(l_baseAddressString, l_dataString[0], l_dataString[1], l_dataString[2], l_dataString[3], l_dataString[4], l_dataString[5], l_dataString[6], l_dataString[7]);

                stackPanelMemoryView.Children.Add(l_viewEntry);
                m_dataEntries.Add(l_viewEntry);
            }
        }

        private void UpdateInstructionView()
        {

        }

        private void UpdateDataView()
        {

        }
    }
}
