using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for TerminalWindow.xaml
    /// </summary>
    public partial class TerminalWindow : Window
    {
        String m_debug_displayString = "0123456789 _-+=/|\\,.:;'\"!@#$%^&*()[]{}qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM<>?`~◌";
        String m_debug_text;

        public TerminalWindow()
        {
            InitializeComponent();

            m_debug_text = "";
            while (m_debug_text.Length < 128*32)
            {
                int l_debug_delta = 128 * 32 - m_debug_text.Length;

                if (l_debug_delta > m_debug_displayString.Length)
                {
                    m_debug_text += m_debug_displayString;
                }
                else
                {
                    m_debug_text += m_debug_displayString.Substring(0, l_debug_delta);
                }
            }

            String[] l_debug_rows = new String[32];
            int l_debug_rowStart = 0;
            for (int i_row = 0; i_row < 32; ++i_row)
            {
                l_debug_rows[i_row] = m_debug_text.Substring(l_debug_rowStart, 128);
                l_debug_rows[i_row] += "\n";
                l_debug_rowStart += 128;
            }
            m_debug_text = "";
            for (int i_row = 0; i_row < 32; ++i_row)
            {
                m_debug_text += l_debug_rows[i_row];
            }

            textBoxTerminal.Text = m_debug_text;
            textBoxTerminal.CaretIndex = 0;
            textBoxTerminal.Focus();
        }

        private void textBoxTerminal_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true; // hijack the scroll event to disable scrolling
        }
    }
}
