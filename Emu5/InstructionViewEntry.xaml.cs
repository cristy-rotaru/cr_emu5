using System;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for InstructionViewEntry.xaml
    /// </summary>
    public partial class InstructionViewEntry : UserControl
    {
        public InstructionViewEntry()
        {
            InitializeComponent();
        }

        public void DisplayData(bool breakpoint, String address, String instruction, String rawValue, String annotation)
        {
            buttonToggleBreakpoint.Content = breakpoint ? new Ellipse { Height = 11, Width = 11 } : null;

            textBlockAddress.Text = address;
            textBlockInstruction.Text = instruction;
            textBlockRawValue.Text = rawValue;
            textBlockAnnotation.Text = annotation;
        }
    }
}
