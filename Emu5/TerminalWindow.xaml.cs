using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for TerminalWindow.xaml
    /// </summary>
    public partial class TerminalWindow : Window
    {
        const Char c_unprintableCharacter = '◌';

        String[] m_lines;
        int m_caretLine, m_caretColumn;

        public TerminalWindow()
        {
            InitializeComponent();

            m_caretLine = 0;
            m_caretColumn = 0;

            m_lines = new String[32];
            for (int i_line = 0; i_line < 32; ++i_line)
            {
                m_lines[i_line] = "                                                                                                                                "; // 128 empty spaces
                if (i_line < 31)
                {
                    m_lines[i_line] += '\n';
                }
            }

            UpdateTextView();
            UpdateCaretPosition();
        }

        private void UpdateCaretPosition()
        {
            int l_caretPosition = m_caretLine * 129; // 129 to take \n into consideration
            l_caretPosition += m_caretColumn;

            textBoxTerminal.CaretIndex = l_caretPosition;
        }

        private void UpdateTextView()
        {
            String l_newText = "";
            foreach (String i_line in m_lines)
            {
                l_newText += i_line;
            }

            textBoxTerminal.Text = l_newText;
        }

        private void ScrollUpAndAddNewLine()
        {
            for (int i_line = 0; i_line < 31; ++i_line)
            {
                m_lines[i_line] = m_lines[i_line + 1];
            }

            m_lines[30] += '\n';
            m_lines[31] = "                                                                                                                                "; // 128 empty spaces
        }

        private void AdvanceCaret()
        {
            ++m_caretColumn;
            if (m_caretColumn > 127)
            {
                m_caretColumn = 0;

                if (m_caretLine < 31)
                {
                    ++m_caretLine;
                }
                else
                {
                    ScrollUpAndAddNewLine();
                }
            }
        }

        private void textBoxTerminal_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true; // hijack the scroll event to disable scrolling
        }

        private void textBoxTerminal_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            { // paste from clipboard
                if (Clipboard.ContainsText())
                {
                    String l_clipboardText = Clipboard.GetText(TextDataFormat.Text);

                    foreach (char i_character in l_clipboardText)
                    {
                        if (i_character <= (char)255)
                        { // ascii extended
                            if (i_character == (char)10) // line feed
                            {
                                if (m_caretLine < 31)
                                {
                                    ++m_caretLine;
                                }
                                else
                                {
                                    ScrollUpAndAddNewLine();
                                }
                            }
                            else if (i_character == (char)13) // carriage return
                            {
                                m_caretColumn = 0;
                            }
                            else if (i_character == (char)9) // tab
                            {
                                m_caretColumn += 8;
                                if (m_caretColumn > 127)
                                {
                                    m_caretColumn = 0;

                                    if (m_caretLine < 31)
                                    {
                                        ++m_caretLine;
                                    }
                                    else
                                    {
                                        ScrollUpAndAddNewLine();
                                    }
                                }
                                else
                                {
                                    m_caretColumn = 8 * (m_caretColumn / 8);
                                }
                            }
                            else if ((i_character >= (char)32 && i_character <= (char)126) || (i_character == (char)128) || (i_character >= (char)130 && i_character <= (char)140) || (i_character == (char)142) || (i_character >= (char)145 && i_character <= (char)156) || (i_character == (char)158) || (i_character == (char)159) || (i_character >= (char)161 && i_character <= (char)255))
                            {
                                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                                l_stringEditor[m_caretColumn] = i_character;
                                m_lines[m_caretLine] = l_stringEditor.ToString();

                                AdvanceCaret();
                            }
                            else
                            {
                                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                                l_stringEditor[m_caretColumn] = c_unprintableCharacter;
                                m_lines[m_caretLine] = l_stringEditor.ToString();

                                AdvanceCaret();
                            }
                        }
                    }

                    UpdateTextView();
                    UpdateCaretPosition();
                }
            }
        }

        private void textBoxTerminal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (textBoxTerminal.SelectionLength != 0)
                {
                    Clipboard.SetText(textBoxTerminal.SelectedText);
                }
            }

            textBoxTerminal.Focus();
            UpdateCaretPosition(); // make sure the caret doesn't change position
        }

        private void textBoxTerminal_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool l_textUpdateRequired = false;

            if (e.Key == Key.Return)
            {
                if (m_caretLine < 31)
                {
                    ++m_caretLine;
                }
                else
                {
                    ScrollUpAndAddNewLine();
                    l_textUpdateRequired = true;
                }

                if (Keyboard.IsKeyDown(Key.LeftShift) == false && Keyboard.IsKeyDown(Key.RightShift) == false)
                { // if the user holds shift the caret will keep it's column
                    m_caretColumn = 0;
                }
            }
            else if (e.Key == Key.Tab)
            {
                m_caretColumn += 8;
                if (m_caretColumn > 127)
                {
                    m_caretColumn = 0;

                    if (m_caretLine < 31)
                    {
                        ++m_caretLine;
                    }
                    else
                    {
                        ScrollUpAndAddNewLine();
                        l_textUpdateRequired = true;
                    }
                }
                else
                {
                    m_caretColumn = 8 * (m_caretColumn / 8);
                }
            }
            else if (e.Key == Key.Space)
            {
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = ' ';
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Back)
            {
                if (m_caretColumn > 0)
                {
                    --m_caretColumn;

                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = ' ';
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = c_unprintableCharacter;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Up)
            {
                if (m_caretLine > 0)
                {
                    --m_caretLine;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (m_caretLine < 31)
                {
                    ++m_caretLine;
                }
            }
            else if (e.Key == Key.Left)
            {
                if (m_caretColumn > 0)
                {
                    --m_caretColumn;
                }
            }
            else if (e.Key == Key.Right)
            {
                if (m_caretColumn < 127)
                {
                    ++m_caretColumn;
                }
            }
            else if (e.Key == Key.Oem1) // the ; and : button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? ':' : ';';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Oem2) // the / and ? button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '?' : '/';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Oem3) // the ` and ~ button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '~' : '`';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Oem4) // the [ and { button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '{' : '[';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Oem5 || e.Key == Key.Oem102) // the \ and | button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '|' : '\\';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Oem6) // the ] and } button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '}' : ']';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Oem7) // the ' and " button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '"' : '\'';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.OemComma) // the , and < button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '<' : ',';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.OemPeriod) // the . and > button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '>' : '.';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.OemMinus) // the - and _ button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '_' : '-';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.OemPlus) // the = and + button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '+' : '=';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D1)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '!' : '1';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D2)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '@' : '2';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D3)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '#' : '3';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D4)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '$' : '4';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D5)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '%' : '5';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D6)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '^' : '6';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D7)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '&' : '7';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D8)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '*' : '8';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D9)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '(' : '9';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.D0)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? ')' : '0';
                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }
            else if (e.Key == Key.Decimal)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = '.';
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    AdvanceCaret();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key == Key.Add)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = '+';
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    AdvanceCaret();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key == Key.Subtract)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = '-';
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    AdvanceCaret();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key == Key.Multiply)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = '*';
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    AdvanceCaret();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key == Key.Divide)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = '/';
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    AdvanceCaret();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    Char l_character = (char)(e.Key - Key.NumPad0 + '0');

                    StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                    l_stringEditor[m_caretColumn] = l_character;
                    m_lines[m_caretLine] = l_stringEditor.ToString();

                    AdvanceCaret();

                    l_textUpdateRequired = true;
                }
            }
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                bool l_capitalize = Keyboard.IsKeyToggled(Key.CapsLock);
                l_capitalize ^= Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                Char l_character = (char)(e.Key - Key.A + (l_capitalize ? 'A' : 'a'));

                StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
                l_stringEditor[m_caretColumn] = l_character;
                m_lines[m_caretLine] = l_stringEditor.ToString();

                AdvanceCaret();

                l_textUpdateRequired = true;
            }

            if (l_textUpdateRequired)
            {
                UpdateTextView();
            }
            UpdateCaretPosition();

            textBoxTerminal.Focus();

            e.Handled = true;
        }
    }
}
