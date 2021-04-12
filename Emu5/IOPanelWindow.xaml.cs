using System;
using System.Windows;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for IOPanelWindow.xaml
    /// </summary>
    public partial class IOPanelWindow : Window
    {
        RVEmulator m_emulator;

        public IOPanelWindow(RVEmulator emulator)
        {
            InitializeComponent();

            m_emulator = emulator;
        }
    }
}
