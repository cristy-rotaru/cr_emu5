using System;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for EmulatorPerspective.xaml
    /// </summary>
    public partial class EmulatorPerspective : UserControl
    {
        RVEmulator m_emulator = null;

        public EmulatorPerspective()
        {
            InitializeComponent();
        }

        public void BindEmulator(RVEmulator emulator)
        {

        }
    }
}
