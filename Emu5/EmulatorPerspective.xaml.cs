using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        RVLabelReferenceMap m_labelMap;

        UInt32 m_currentPC;
        UInt32[] m_previousRegisterValues;
        TextBlock[] m_registerTextBoxes;

        Timer m_resizeTimer;

        bool m_highlightingEnabled = true;

        public bool HighlightingEnabled
        {
            set
            {
                m_highlightingEnabled = value;

                foreach (InstructionViewEntry i_viewEntry in m_instructionEntries)
                {
                    UInt32 l_address = i_viewEntry.GetAddress();

                    i_viewEntry.Highlighted = m_highlightingEnabled && (l_address == m_currentPC);
                }
            }
        }

        public EmulatorPerspective()
        {
            InitializeComponent();

            m_instructionEntries = new List<InstructionViewEntry>();
            m_dataEntries = new List<DataViewEntry>();

            m_labelMap = new RVLabelReferenceMap();

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

            m_resizeTimer = new Timer(65);
            m_resizeTimer.Elapsed += new ElapsedEventHandler(ResizeComplete);
            m_resizeTimer.AutoReset = false;

            UpdateInfo();
        }

        public void BindEmulator(RVEmulator emulator)
        {
            m_emulator = emulator;
        }

        public void SetLabelReferences(RVLabelReferenceMap labelMap)
        {
            m_labelMap = labelMap;
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

            stackPanelInstructionView.UpdateLayout();

            UInt32 l_normalizedPC = m_currentPC & ~(UInt32)0x3;
            int l_entryCount = (int)(stackPanelInstructionView.ActualHeight / 24);
            for (int i_index = 0; i_index < l_entryCount; ++i_index)
            {
                InstructionViewEntry l_viewEntry = new InstructionViewEntry(ConfigureBreakpoint);

                UInt32 l_address = l_normalizedPC + (UInt32)(i_index << 2);
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().Read(l_address, 4);
                
                l_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_address), l_address, l_rawData, m_labelMap);

                l_viewEntry.Highlighted = m_highlightingEnabled && (l_address == m_currentPC);

                stackPanelInstructionView.Children.Add(l_viewEntry);
                m_instructionEntries.Add(l_viewEntry);
            }
        }

        private void RefreshDataView()
        {
            UInt32 l_startAddress = 0x0;
            if (m_dataEntries.Count > 0)
            {
                l_startAddress = m_dataEntries[0].GetBaseAddress();
            }

            stackPanelMemoryView.Children.Clear();
            m_dataEntries.Clear();

            stackPanelMemoryView.UpdateLayout();

            int l_entryCount = (int)(stackPanelMemoryView.ActualHeight / 24);
            for (int i_index = 0; i_index < l_entryCount; ++i_index)
            {
                DataViewEntry l_viewEntry = new DataViewEntry();

                UInt32 l_baseAddress = (UInt32)(l_startAddress + i_index << 3);
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().Read(l_baseAddress, 8);
                
                l_viewEntry.DisplayData(l_baseAddress, l_rawData);

                stackPanelMemoryView.Children.Add(l_viewEntry);
                m_dataEntries.Add(l_viewEntry);
            }
        }

        private void UpdateInstructionView()
        {
            int l_entryCount = m_instructionEntries.Count;
            if (l_entryCount == 0)
            {
                RefreshInstructionView();
            }

            UInt32 l_firstAddress = m_instructionEntries[0].GetAddress();
            UInt32 l_lastAddress = m_instructionEntries[l_entryCount - 1].GetAddress();

            bool l_refreshRequired = false;
            if (l_firstAddress < l_lastAddress)
            {
                if (m_currentPC < l_firstAddress || m_currentPC > l_lastAddress)
                {
                    l_refreshRequired = true;
                }
            }
            else
            {
                if (m_currentPC < l_firstAddress && m_currentPC > l_lastAddress)
                {
                    l_refreshRequired = true;
                }
            }

            if (l_refreshRequired)
            {
                RefreshInstructionView();
            }
            else
            {
                foreach (InstructionViewEntry i_viewEntry in m_instructionEntries)
                {
                    UInt32 l_address = i_viewEntry.GetAddress();
                    i_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_address), l_address, m_emulator.GetMemoryMapReference().Read(l_address, 4), m_labelMap);
                    i_viewEntry.Highlighted = m_highlightingEnabled && (l_address == m_currentPC);
                }
            }
        }

        private void UpdateDataView()
        {
            foreach (DataViewEntry i_viewEntry in m_dataEntries)
            {
                UInt32 l_baseAddress = i_viewEntry.GetBaseAddress();
                i_viewEntry.DisplayData(l_baseAddress, m_emulator.GetMemoryMapReference().Read(l_baseAddress, 8));
            }
        }

        private void textBoxTargetInstructionAddress_KeyDown(object sender, KeyEventArgs e)
        { // go to address
            if (e.Key == Key.Enter)
            {
                if (m_emulator == null)
                {
                    MessageBox.Show("You need to start a simulation to see memory contents.", "No simulation running", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    try
                    {
                        UInt32 l_address = UInt32.Parse(textBoxTargetInstructionAddress.Text, NumberStyles.HexNumber);
                        l_address &= ~(UInt32)0x3; // normalize address

                        foreach (InstructionViewEntry i_viewEntry in m_instructionEntries)
                        {
                            byte?[] l_rawData = m_emulator.GetMemoryMapReference().Read(l_address, 4);

                            i_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_address), l_address, l_rawData, m_labelMap);
                            i_viewEntry.Highlighted = m_highlightingEnabled && (l_address == m_currentPC);

                            l_address += 4;
                        }    
                    }
                    catch (Exception e_numberConversionError)
                    {
                        MessageBox.Show(e_numberConversionError.Message, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                textBoxTargetInstructionAddress.Clear();
            }
        }

        private void textBoxTargetDataAddress_KeyDown(object sender, KeyEventArgs e)
        { // go to address
            if (e.Key == Key.Enter)
            {
                if (m_emulator == null)
                {
                    MessageBox.Show("You need to start a simulation to see memory contents.", "No simulation running", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    try
                    {
                        UInt32 l_address = UInt32.Parse(textBoxTargetDataAddress.Text, NumberStyles.HexNumber);
                        l_address &= ~(UInt32)0x7; // normalize address

                        foreach (DataViewEntry i_viewEntry in m_dataEntries)
                        {
                            byte?[] l_rawData = m_emulator.GetMemoryMapReference().Read(l_address, 8);

                            i_viewEntry.DisplayData(l_address, l_rawData);

                            l_address += 8;
                        }
                    }
                    catch (Exception e_numberConversionError)
                    {
                        MessageBox.Show(e_numberConversionError.Message, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                textBoxTargetDataAddress.Clear();
            }
        }

        private void EmulatorPerspective_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                m_resizeTimer.Stop();
                m_resizeTimer.Start();
            }
        }

        private void ResizeComplete(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                if (m_emulator == null)
                {
                    m_instructionEntries.Clear();
                    stackPanelInstructionView.Children.Clear();

                    m_dataEntries.Clear();
                    stackPanelMemoryView.Children.Clear();
                }
                else
                {
                    RefreshInstructionView();
                    RefreshDataView();
                }
            }));
        }

        private void stackPanelInstructionView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_instructionEntries.Count == 0)
            {
                return;
            }

            UInt32 l_nextAddress;
            int l_addIndex;

            if (e.Delta > 0) // scroll up
            {
                int l_lastIndex = m_instructionEntries.Count - 1;

                for (int i_index = l_lastIndex; i_index > 0; --i_index)
                {
                    m_instructionEntries[i_index] = m_instructionEntries[i_index - 1];
                }

                l_nextAddress = m_instructionEntries[0].GetAddress() - 4;
                m_instructionEntries.RemoveAt(0);
                l_addIndex = 0;
            }
            else // scroll down
            {
                int l_lastIndex = m_instructionEntries.Count - 1;

                for (int i_index = 0; i_index < l_lastIndex; ++i_index)
                {
                    m_instructionEntries[i_index] = m_instructionEntries[i_index + 1];
                }

                l_nextAddress = m_instructionEntries[l_lastIndex].GetAddress() + 4;
                m_instructionEntries.RemoveAt(l_lastIndex);
                l_addIndex = l_lastIndex;
            }

            InstructionViewEntry l_viewEntry = new InstructionViewEntry(ConfigureBreakpoint);
            l_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_nextAddress), l_nextAddress, m_emulator.GetMemoryMapReference().Read(l_nextAddress, 4), m_labelMap);
            l_viewEntry.Highlighted = m_highlightingEnabled && (l_nextAddress == m_currentPC);

            m_instructionEntries.Insert(l_addIndex, l_viewEntry);
            stackPanelInstructionView.Children.Clear();

            foreach (InstructionViewEntry i_entry in m_instructionEntries)
            {
                stackPanelInstructionView.Children.Add(i_entry);
            }
        }

        private void stackPanelMemoryView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_dataEntries.Count == 0)
            {
                return;
            }

            UInt32 l_nextAddress;
            int l_addIndex;

            if (e.Delta > 0) // scroll up
            {
                int l_lastIndex = m_dataEntries.Count - 1;

                for (int i_index = l_lastIndex; i_index > 0; --i_index)
                {
                    m_dataEntries[i_index] = m_dataEntries[i_index - 1];
                }

                l_nextAddress = m_dataEntries[0].GetBaseAddress() - 8;
                m_dataEntries.RemoveAt(0);
                l_addIndex = 0;
            }
            else // scroll down
            {
                int l_lastIndex = m_dataEntries.Count - 1;

                for (int i_index = 0; i_index < l_lastIndex; ++i_index)
                {
                    m_dataEntries[i_index] = m_dataEntries[i_index + 1];
                }

                l_nextAddress = m_dataEntries[l_lastIndex].GetBaseAddress() + 8;
                m_dataEntries.RemoveAt(l_lastIndex);
                l_addIndex = l_lastIndex;
            }

            DataViewEntry l_viewEntry = new DataViewEntry();
            l_viewEntry.DisplayData(l_nextAddress, m_emulator.GetMemoryMapReference().Read(l_nextAddress, 8));

            m_dataEntries.Insert(l_addIndex, l_viewEntry);
            stackPanelMemoryView.Children.Clear();

            foreach (DataViewEntry i_entry in m_dataEntries)
            {
                stackPanelMemoryView.Children.Add(i_entry);
            }
        }

        private void ConfigureBreakpoint(bool active, UInt32 address)
        {
            if (m_emulator != null)
            {
                if (active)
                {
                    m_emulator.AddBreakpoint(address);
                }
                else
                {
                    m_emulator.RemoveBreakpoint(address);
                }
            }
        }
    }
}
