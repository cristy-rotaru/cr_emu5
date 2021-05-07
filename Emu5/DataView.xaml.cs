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
    /// Interaction logic for DataView.xaml
    /// </summary>
    public partial class DataView : UserControl
    {
        RVEmulator m_emulator = null;

        List<DataViewEntry> m_dataEntries;

        Timer m_resizeTimer;

        public DataView()
        {
            InitializeComponent();

            m_dataEntries = new List<DataViewEntry>();

            m_resizeTimer = new Timer(65);
            m_resizeTimer.Elapsed += new ElapsedEventHandler(ResizeComplete);
            m_resizeTimer.AutoReset = false;
        }

        public void BindEmulator(RVEmulator emulator)
        {
            m_emulator = emulator;
        }

        public void UpdateInfo()
        {
            if (m_emulator == null)
            {
                m_dataEntries.Clear();
                stackPanelMemoryView.Children.Clear();
            }
            else
            {
                if (m_dataEntries.Count == 0)
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
            foreach (DataViewEntry i_viewEntry in m_dataEntries)
            {
                UInt32 l_baseAddress = i_viewEntry.GetBaseAddress();
                i_viewEntry.DisplayData(l_baseAddress, m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_baseAddress, 8));
            }
        }

        private void RefreshView()
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
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_baseAddress, 8);

                l_viewEntry.DisplayData(l_baseAddress, l_rawData);

                stackPanelMemoryView.Children.Add(l_viewEntry);
                m_dataEntries.Add(l_viewEntry);
            }
        }

        private void ResizeComplete(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                if (m_emulator == null)
                {
                    m_dataEntries.Clear();
                    stackPanelMemoryView.Children.Clear();
                }
                else
                {
                    RefreshView();
                }
            }));
        }

        private void DataView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                m_resizeTimer.Stop();
                m_resizeTimer.Start();
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
                            byte?[] l_rawData = m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_address, 8);

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
            l_viewEntry.DisplayData(l_nextAddress, m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_nextAddress, 8));

            m_dataEntries.Insert(l_addIndex, l_viewEntry);
            stackPanelMemoryView.Children.Clear();

            foreach (DataViewEntry i_entry in m_dataEntries)
            {
                stackPanelMemoryView.Children.Add(i_entry);
            }
        }
    }
}
