using System;

namespace Emu5
{
    class InterruptInjector : I_RVPeripheral
    {
        bool m_invalidProgrammingDetected;

        RVEmulator m_emulator;

        public InterruptInjector(RVEmulator emulator)
        {
            m_invalidProgrammingDetected = false;

            m_emulator = emulator;
        }

        void I_RVPeripheral.Reset()
        {
            m_invalidProgrammingDetected = false;
        }

        byte[] I_RVPeripheral.ReadRegisters(UInt32 offset, int count)
        {
            if (offset + count > 4)
            {
                throw new Exception("Invalid register offset");
            }

            byte[] l_toReturn = new byte[count];
            for (int i_byteIndex = 0; i_byteIndex < count; ++i_byteIndex)
            {
                l_toReturn[i_byteIndex] = 0;
            }

            return l_toReturn;
        }

        void I_RVPeripheral.WriteRegisters(UInt32 offset, byte[] data)
        {
            if (offset + data.Length > 4)
            {
                throw new Exception("Invalid register offset");
            }

            if (m_invalidProgrammingDetected)
            {
                return;
            }

            if (offset != 0 && data.Length != 4)
            {
                m_invalidProgrammingDetected = true;
                return;
            }

            UInt32 l_command = 0;
            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
            {
                l_command <<= 8;
                l_command |= (UInt32)data[i_byteIndex];
            }

            /* Command format:
             * command[31:24] must be 0
             * command[23:16] - interrupt_code
             * command[15:0]  - interrupt_delay
             * 
             * interrupt_code:
             * 0b11100000 - reset
             * 0b00000001 - NMI
             * 0b00000010 to 0b00000111 - invalid
             * 0b00001000 to 0b00011111 - valid interrupt vector
             * 0b00100000 to 0b11011111 - invalid
             * 0b11100001 to 0b11111111 - invalid
             */

            if ((l_command & 0xFF000000) != 0)
            {
                m_invalidProgrammingDetected = true;
                return;
            }

            byte l_interruptCode = (byte)(l_command >> 16);
            ushort l_interruptDelay = (ushort)l_command;

            if (l_interruptCode == 0b11100000) // reset
            {
                m_emulator.QueueExternalInterrupt(RVVector.Reset, l_interruptDelay);
            }
            else if ((l_interruptCode & 0b11100000) != 0) // not a valid interrupt
            {
                m_invalidProgrammingDetected = true;
            }
            else if (l_interruptCode > 1 && l_interruptCode < 8) // exception vectors cannot be injected
            {
                m_invalidProgrammingDetected = true;
            }
            else
            {
                m_emulator.QueueExternalInterrupt((RVVector)l_interruptCode, l_interruptDelay);
            }
        }
    }
}
