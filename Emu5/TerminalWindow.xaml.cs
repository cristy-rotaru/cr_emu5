using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for TerminalWindow.xaml
    /// </summary>
    public partial class TerminalWindow : Window
    {
        Terminal m_terminal;

        int m_caretPosition;

        bool m_initialized = false;

        public TerminalWindow(Terminal peripheral)
        {
            InitializeComponent();

            m_terminal = peripheral;
            m_terminal.RegisterTextChangedCallback(UpdateTextAndCaret);

            m_caretPosition = 0;

            m_initialized = true;
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

                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_clipboardText.ToCharArray());
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
            textBoxTerminal.CaretIndex = m_caretPosition;
        }

        private void textBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (radioButtonHex.IsChecked == true)
                {
                    ParseAndSendHex();
                }
                else if (radioButtonString.IsChecked == true)
                {
                    SendString();
                }
            }
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            if (radioButtonHex.IsChecked == true)
            {
                ParseAndSendHex();
            }
            else if (radioButtonString.IsChecked == true)
            {
                SendString();
            }
        }

        private void textBoxTerminal_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (radioButtonKeyboard.IsChecked != true)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Return)
            {
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '\n');

                if ((Keyboard.IsKeyDown(Key.LeftShift) == false && Keyboard.IsKeyDown(Key.RightShift) == false) ^ checkBoxNewLineSetsCaretTo0.IsChecked == false)
                { // if the user holds shift the caret will keep it's column
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '\r');
                }
            }
            else if (e.Key == Key.Tab)
            {
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '\t');
            }
            else if (e.Key == Key.Space)
            {
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, ' ');
            }
            else if (e.Key == Key.Back)
            {
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '\b');
            }
            else if (e.Key == Key.Escape)
            {
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, (char)27); // escape
            }
            else if (e.Key == Key.Oem1) // the ; and : button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? ':' : ';';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Oem2) // the / and ? button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '?' : '/';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Oem3) // the ` and ~ button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '~' : '`';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Oem4) // the [ and { button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '{' : '[';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Oem5 || e.Key == Key.Oem102) // the \ and | button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '|' : '\\';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Oem6) // the ] and } button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '}' : ']';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Oem7) // the ' and " button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '"' : '\'';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.OemComma) // the , and < button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '<' : ',';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.OemPeriod) // the . and > button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '>' : '.';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.OemMinus) // the - and _ button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '_' : '-';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.OemPlus) // the = and + button
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '+' : '=';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D1)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '!' : '1';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D2)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '@' : '2';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D3)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '#' : '3';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D4)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '$' : '4';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D5)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '%' : '5';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D6)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '^' : '6';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D7)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '&' : '7';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D8)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '*' : '8';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D9)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? '(' : '9';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.D0)
            {
                Char l_character = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? ')' : '0';
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }
            else if (e.Key == Key.Decimal)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '.');
                }
            }
            else if (e.Key == Key.Add)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '+');
                }
            }
            else if (e.Key == Key.Subtract)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '-');
                }
            }
            else if (e.Key == Key.Multiply)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '*');
                }
            }
            else if (e.Key == Key.Divide)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, '/');
                }
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                if (Keyboard.IsKeyToggled(Key.NumLock))
                {
                    Char l_character = (char)(e.Key - Key.NumPad0 + '0');
                    m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
                }
            }
            else if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                bool l_capitalize = Keyboard.IsKeyToggled(Key.CapsLock);
                l_capitalize ^= Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                Char l_character = (char)(e.Key - Key.A + (l_capitalize ? 'A' : 'a'));
                m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_character);
            }

            textBoxTerminal.Focus();

            e.Handled = true;
        }

        private void radioButtonKeyboard_Checked(object sender, RoutedEventArgs e)
        {
            if (m_initialized)
            {
                textBoxInput.Clear();

                textBoxInput.IsEnabled = false;
                buttonSend.IsEnabled = false;

                textBoxTerminal.Focus();
            }
        }

        private void radioButtonHexString_Checked(object sender, RoutedEventArgs e)
        {
            if (m_initialized)
            {
                textBoxInput.IsEnabled = true;
                buttonSend.IsEnabled = true;

                textBoxInput.Focus();
            }
        }

        private void checkBox_Toggled(object sender, RoutedEventArgs e)
        {
            if (m_initialized)
            {
                if (radioButtonKeyboard.IsChecked == true)
                {
                    textBoxTerminal.Focus();
                }
                else
                {
                    textBoxInput.Focus();
                }

                m_terminal.BackspaceDeletesCharacter = checkBoxBackspaceDeletesCharacters.IsChecked == true;
            }
        }

        private void UpdateTextAndCaret(String text, int caretPosition)
        {
            textBoxTerminal.Text = text;
            textBoxTerminal.CaretIndex = caretPosition;

            m_caretPosition = caretPosition;
        }

        private void ParseAndSendHex()
        {
            String[] l_tokens = textBoxInput.Text.Split(' ');
            char[] l_characters = new char[l_tokens.Length];

            for (int i_token = 0; i_token < l_tokens.Length; ++i_token)
            {
                try
                {
                    byte l_characterCode = byte.Parse(l_tokens[i_token], NumberStyles.HexNumber);
                    l_characters[i_token] = (char)l_characterCode;
                }
                catch
                {
                    MessageBox.Show(l_tokens[i_token] + " is not a valid ascii extended character hex code.", "Invalid character code", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, l_characters);

            textBoxInput.Clear();
        }

        private void SendString()
        {
            m_terminal.SendCharacters(checkBoxEchoSentCharacters.IsChecked == true, textBoxInput.Text.ToCharArray());

            textBoxInput.Clear();
        }
    }
}
