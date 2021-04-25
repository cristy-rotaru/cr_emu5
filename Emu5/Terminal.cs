using System;
using System.Collections.Generic;
using System.Text;

namespace Emu5
{
    class Terminal : I_RVPeripheral
    {
        public delegate void UpdateTextDelgate(String text, int caretPosition);

        RVEmulator m_emulator;

        UpdateTextDelgate m_textChangedHandler;

        const Char c_unprintableCharacter = '◌';

        String[] m_lines;
        int m_caretLine, m_caretColumn;

        Queue<byte> m_characterBuffer;
        byte m_lastCharacterRead;

        bool m_triggerInterruptOnSend;

        public Terminal(RVEmulator emulator)
        {
            m_emulator = emulator;

            m_characterBuffer = new Queue<byte>();
            m_lastCharacterRead = 0x00;
            m_triggerInterruptOnSend = false;

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

            m_textChangedHandler = null;
        }

        void I_RVPeripheral.Reset()
        {
            m_characterBuffer.Clear();
            m_lastCharacterRead = 0x00;

            m_caretLine = 0;
            m_caretColumn = 0;

            for (int i_line = 0; i_line < 32; ++i_line)
            {
                m_lines[i_line] = "                                                                                                                                "; // 128 empty spaces
                if (i_line < 31)
                {
                    m_lines[i_line] += '\n';
                }
            }
        }

        byte[] I_RVPeripheral.ReadRegisters(uint offset, int count)
        {
            byte[] l_result = new byte[count];

            for (int i_byteIndex = 0; i_byteIndex < count; ++i_byteIndex)
            {
                int l_registerOffset = (int)offset + i_byteIndex;

                switch (l_registerOffset)
                {
                    case 0: // status and control
                    {
                        byte l_status = m_triggerInterruptOnSend ? (byte)0x80 : (byte)0x00;
                        l_status |= (byte)(m_characterBuffer.Count & 0x7F);

                        l_result[i_byteIndex] = l_status;
                    }
                    break;

                    case 1: // data register
                    {
                        if (m_characterBuffer.Count > 0)
                        {
                            m_lastCharacterRead = m_characterBuffer.Dequeue();
                        }

                        l_result[i_byteIndex] = m_lastCharacterRead;
                    }
                    break;

                    case 2: // caret column
                    {
                        l_result[i_byteIndex] = (byte)m_caretColumn;
                    }
                    break;

                    case 3: // caret line
                    {
                        l_result[i_byteIndex] = (byte)m_caretLine;
                    }
                    break;

                    default:
                    {
                        throw new Exception("Invalid register offset");
                    }
                }
            }

            return l_result;
        }

        void I_RVPeripheral.WriteRegisters(uint offset, byte[] data)
        {
            bool l_textOrCaretChanged = false;

            for (int i_byteIndex = 0; i_byteIndex < data.Length; ++i_byteIndex)
            {
                int l_registerOffset = (int)offset + i_byteIndex;

                switch (l_registerOffset)
                {
                    case 0: // status and control
                    {
                        m_triggerInterruptOnSend = (data[i_byteIndex] & 0x80) != 0;
                        if ((data[i_byteIndex] & 0x40) != 0)
                        {
                            m_characterBuffer.Clear();
                        }
                    }
                    break;

                    case 1: // data register
                    {
                        TryPrint((char)data[i_byteIndex]);
                        l_textOrCaretChanged = true;
                    }
                    break;

                    case 2: // caret column
                    {
                        if (data[i_byteIndex] < 128)
                        {
                            m_caretColumn = (int)data[i_byteIndex];
                            l_textOrCaretChanged = true;
                        }
                    }
                    break;

                    case 3: // caret line
                    {
                        if (data[i_byteIndex] < 32)
                        {
                            m_caretLine = (int)data[i_byteIndex];
                            l_textOrCaretChanged = true;
                        }
                    }
                    break;

                    default:
                    {
                        throw new Exception("Invalid register offset");
                    }
                }
            }

            if (l_textOrCaretChanged)
            {
                NotifyTextChanged();
            }
        }

        public void RegisterTextChangedCallback(UpdateTextDelgate handler)
        {
            m_textChangedHandler = handler;
            NotifyTextChanged();
        }

        public void SendCharacter(bool putOnScreen, params byte[] characterCodes)
        {
            foreach (byte i_characterCode in characterCodes)
            {
                if (m_characterBuffer.Count == 127) // buffer limit | will be configurable
                {
                    m_characterBuffer.Dequeue(); // remove oldest character | buffer full policy will also be configurable
                }

                m_characterBuffer.Enqueue(i_characterCode);
                if (putOnScreen)
                {
                    TryPrint((char)i_characterCode);
                }

                if (m_triggerInterruptOnSend)
                {
                    m_emulator.QueueExternalInterrupt(RVVector.External9, 0);
                }
            }

            if (putOnScreen)
            {
                NotifyTextChanged();
            }
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

        private void TryPrint(char character)
        {
            if (character <= (char)255)
            { // ascii extended
                if (character == (char)10) // line feed
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
                else if (character == (char)13) // carriage return
                {
                    m_caretColumn = 0;
                }
                else if (character == (char)9) // tab
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
                else if ((character >= (char)32 && character <= (char)126) || (character >= (char)161 && character <= (char)172) || (character >= (char)174 && character <= (char)255))
                {
                    PutChar(character);
                }
                else
                {
                    PutChar(c_unprintableCharacter);
                }
            }
            else
            {
                PutChar(c_unprintableCharacter);
            }
        }

        private void PutChar(char character)
        {
            StringBuilder l_stringEditor = new StringBuilder(m_lines[m_caretLine]);
            l_stringEditor[m_caretColumn] = character;
            m_lines[m_caretLine] = l_stringEditor.ToString();

            AdvanceCaret();
        }

        private void NotifyTextChanged()
        {
            if (m_textChangedHandler != null)
            {
                String l_text = "";
                foreach (String i_line in m_lines)
                {
                    l_text += i_line;
                }

                int l_caretPosition = m_caretLine * 129; // 129 to take \n into consideration
                l_caretPosition += m_caretColumn;

                m_textChangedHandler(l_text, l_caretPosition);
            }
        }
    }
}
