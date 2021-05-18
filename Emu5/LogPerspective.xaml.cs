using System;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for LogPerspective.xaml
    /// </summary>
    public partial class LogPerspective : UserControl
    {
        public LogPerspective()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            textBoxLog.Clear();
        }

        public void Log(String text)
        {
            textBoxLog.AppendText(text + "\n");
        }

        public void NewLine()
        {
            textBoxLog.AppendText("\n");
        }
    }
}
