using System;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for LogPerspective.xaml
    /// </summary>
    public partial class LogPerspective : UserControl
    {
        String m_log;

        public LogPerspective()
        {
            InitializeComponent();

            m_log = "";
        }

        public void Clear()
        {
            lock (m_log)
            {
                m_log = "";
            }
        }

        public void Log(String text)
        {
            lock (m_log)
            {
                m_log += text + '\n';
            }
        }

        public void NewLine()
        {
            lock (m_log)
            {
                m_log += '\n';
            }
        }

        public void UpdateLogUI()
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                lock (m_log)
                {
                    textBoxLog.Text = m_log;
                }
            }));
        }
    }
}
