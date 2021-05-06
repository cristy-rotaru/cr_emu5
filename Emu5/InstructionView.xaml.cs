using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for InstructionView.xaml
    /// </summary>
    public partial class InstructionView : UserControl
    {
        RVEmulator m_emulator = null;

        UInt32 m_currentPC;

        List<InstructionViewEntry> m_instructionEntries;
        
        RVLabelReferenceMap m_labelMap;
        Dictionary<UInt32, String> m_pseudoInstructionMap;

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

        public InstructionView()
        {
            InitializeComponent();

            m_currentPC = 0x0;

            m_instructionEntries = new List<InstructionViewEntry>();

            m_labelMap = new RVLabelReferenceMap();
            m_pseudoInstructionMap = new Dictionary<UInt32, String>();

            m_resizeTimer = new Timer(65);
            m_resizeTimer.Elapsed += new ElapsedEventHandler(ResizeComplete);
            m_resizeTimer.AutoReset = false;
        }

        public void BindEmulator(RVEmulator emulator)
        {
            m_emulator = emulator;
        }

        public void SetLabelReferences(RVLabelReferenceMap labelMap)
        {
            m_labelMap = labelMap;
        }

        public void SetPseudoInstructionReference(Dictionary<UInt32, String> pseudoInstructionMap)
        {
            m_pseudoInstructionMap = pseudoInstructionMap;
        }

        public void UpdateInfo()
        {
            if (m_emulator == null)
            {
                m_instructionEntries.Clear();
                stackPanelInstructionView.Children.Clear();
            }
            else
            {
                m_currentPC = m_emulator.GetProgramCounter();

                if (m_instructionEntries.Count == 0)
                {
                    RefreshView();
                }
                else
                {
                    UpdateView();
                }
            }
        }

        private void UpdateView()
        {
            int l_entryCount = m_instructionEntries.Count;
            if (l_entryCount == 0)
            {
                RefreshView();
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
                RefreshView();
            }
            else
            {
                foreach (InstructionViewEntry i_viewEntry in m_instructionEntries)
                {
                    UInt32 l_address = i_viewEntry.GetAddress();
                    i_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_address), l_address, m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_address, 4), m_labelMap, m_pseudoInstructionMap);
                    i_viewEntry.Highlighted = m_highlightingEnabled && (l_address == m_currentPC);
                }
            }
        }

        private void RefreshView()
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
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_address, 4);

                l_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_address), l_address, l_rawData, m_labelMap, m_pseudoInstructionMap);

                l_viewEntry.Highlighted = m_highlightingEnabled && (l_address == m_currentPC);

                stackPanelInstructionView.Children.Add(l_viewEntry);
                m_instructionEntries.Add(l_viewEntry);
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
                }
                else
                {
                    RefreshView();
                }
            }));
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

        private void InstructionView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                m_resizeTimer.Stop();
                m_resizeTimer.Start();
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
                            byte?[] l_rawData = m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_address, 4);

                            i_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_address), l_address, l_rawData, m_labelMap, m_pseudoInstructionMap);
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
            l_viewEntry.DisplayData(m_emulator.HasBreakpoint(l_nextAddress), l_nextAddress, m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_nextAddress, 4), m_labelMap, m_pseudoInstructionMap);
            l_viewEntry.Highlighted = m_highlightingEnabled && (l_nextAddress == m_currentPC);

            m_instructionEntries.Insert(l_addIndex, l_viewEntry);
            stackPanelInstructionView.Children.Clear();

            foreach (InstructionViewEntry i_entry in m_instructionEntries)
            {
                stackPanelInstructionView.Children.Add(i_entry);
            }
        }
    }
}
