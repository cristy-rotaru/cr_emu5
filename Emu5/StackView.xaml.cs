using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for StackView.xaml
    /// </summary>
    public partial class StackView : UserControl
    {
        RVEmulator m_emulator = null;

        List<StackViewEntry> m_stackEntries;

        Timer m_resizeTimer;

        public StackView()
        {
            InitializeComponent();

            m_stackEntries = new List<StackViewEntry>();

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
                m_stackEntries.Clear();
                stackPanelStackView.Children.Clear();
            }
            else
            {
                if (m_stackEntries.Count == 0)
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
            UInt32 l_address = m_emulator.GetRegisterFile()[2];
            bool l_stackTop = true;

            foreach (StackViewEntry i_viewEntry in m_stackEntries)
            {
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_address, 4);

                i_viewEntry.DisplayData(l_address, l_rawData, l_stackTop);

                l_address += 4;
                l_stackTop = false;
            }
        }

        private void RefreshView()
        {
            m_stackEntries.Clear();
            stackPanelStackView.Children.Clear();

            stackPanelStackView.UpdateLayout();

            UInt32 l_stackTop = m_emulator.GetRegisterFile()[2];

            int l_entryCount = (int)(stackPanelStackView.ActualHeight / 82);
            for (int i_index = 0; i_index < l_entryCount; ++i_index)
            {
                StackViewEntry l_viewEntry = new StackViewEntry();

                UInt32 l_address = l_stackTop + (UInt32)(i_index << 2);
                byte?[] l_rawData = m_emulator.GetMemoryMapReference().ReadIgnorePeripherals(l_address, 4);

                l_viewEntry.DisplayData(l_address, l_rawData, i_index == 0);

                stackPanelStackView.Children.Add(l_viewEntry);
                m_stackEntries.Add(l_viewEntry);
            }
        }

        private void ResizeComplete(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                if (m_emulator == null)
                {
                    m_stackEntries.Clear();
                    stackPanelStackView.Children.Clear();
                }
                else
                {
                    RefreshView();
                }
            }));
        }

        private void StackView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                m_resizeTimer.Stop();
                m_resizeTimer.Start();
            }
        }
    }
}
