using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
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

        public void LogText(String text, bool newLine)
        {
            lock (m_log)
            {
                m_log += text + (newLine ? '\n' : ' ');
            }
        }

        public void LogCurrentTime(bool newLine)
        {
            DateTime l_currentTime = DateTime.Now;

            m_log += String.Format("{0:0000}", l_currentTime.Year);
            m_log += '-';
            m_log += String.Format("{0:00}", l_currentTime.Month);
            m_log += '-';
            m_log += String.Format("{0:00}", l_currentTime.Day);
            m_log += ' ';
            m_log += l_currentTime.Hour;
            m_log += ':';
            m_log += String.Format("{0:00}", l_currentTime.Minute);
            m_log += ':';
            m_log += String.Format("{0:00}", l_currentTime.Second);
            m_log += '.';
            m_log += String.Format("{0:000}", l_currentTime.Millisecond);

            m_log += newLine ? '\n' : ' ';
        }

        public void LogByteArray(byte[] bytes, bool newLine)
        {
            if (bytes != null)
            {
                foreach (byte i_byte in bytes)
                {
                    m_log += String.Format("{0, 2:X2}", i_byte) + ' ';
                }
            }

            if (newLine)
            {
                m_log += '\n';
            }
        }

        public void LogByteArray(byte?[] bytes, bool newLine)
        {
            if (bytes != null)
            {
                foreach (byte? i_byte in bytes)
                {
                    m_log += (i_byte == null ? "xx" : String.Format("{0, 2:X2}", i_byte)) + ' ';
                }
            }

            if (newLine)
            {
                m_log += '\n';
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
                    textBoxLog.ScrollToEnd();
                }
            }));
        }

        public void Save()
        {
            SaveFileDialog l_sfd = new SaveFileDialog();
            l_sfd.Filter = "Log file (*.log, *.txt)|*.log;*.txt|All files|*";
            l_sfd.FileName = "SimulationResults.log";

            if (l_sfd.ShowDialog() == true)
            {
                try
                {
                    StreamWriter l_fileWriter = new StreamWriter(l_sfd.FileName, false, Encoding.Unicode);
                    l_fileWriter.Write(textBoxLog.Text);

                    l_fileWriter.Close();
                }
                catch (Exception e_fileSaveException)
                {
                    MessageBox.Show(e_fileSaveException.Message, "Could not save the file", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }
    }
}
