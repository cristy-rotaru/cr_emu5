using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emu5
{
    public class IOPanel : I_RVPeripheral
    {
        RVEmulator m_emulator;

        UInt16 m_segmentValues;
        UInt16 m_buttonEvents;
        UInt16 m_buttonInterruptMask;
        UInt16 m_ledValues;
        UInt16 m_switchValues;

        // synchronizer objects | to ensure correct concurent access
        object m_segmentSync;
        object m_buttonSync;
        object m_ledSync;
        object m_switchSync;

        public IOPanel(RVEmulator emulator)
        {
            m_emulator = emulator;

            m_segmentSync = new object();
            m_buttonSync = new object();
            m_ledSync = new object();
            m_switchSync = new object();

            m_segmentValues = 0x0000;
            m_buttonEvents = 0x0000;
            m_buttonInterruptMask = 0x0000;
            m_ledValues = 0x0000;
        }

        void I_RVPeripheral.Reset()
        {
            lock (m_segmentSync)
            {
                m_segmentValues = 0;
            }

            lock (m_buttonSync)
            {
                m_buttonInterruptMask = 0;
                m_buttonEvents = 0;
            }

            lock (m_ledSync)
            {
                m_ledValues = 0;
            }
        }

        byte[] I_RVPeripheral.ReadRegisters(uint offset, int count)
        {
            byte[] l_result = new byte[count];
            bool l_buttonEventsRead = false;

            for (int i_byteIndex = 0; i_byteIndex < count; ++i_byteIndex)
            {
                int l_registerOffset = (int)offset + i_byteIndex;

                switch (l_registerOffset)
                {
                    case 0: // segments LO
                    {
                        lock (m_segmentSync)
                        {
                            l_result[i_byteIndex] = (byte)m_segmentValues;
                        }
                    }
                    break;

                    case 1: // segments HI
                    {
                        lock (m_segmentSync)
                        {
                            l_result[i_byteIndex] = (byte)(m_segmentValues >> 8);
                        }
                    }
                    break;

                    case 2: // buttons LO
                    {
                        lock (m_buttonSync)
                        {
                            l_result[i_byteIndex] = (byte)m_buttonEvents;
                            l_buttonEventsRead = true;
                        }
                    }
                    break;

                    case 3: // buttons HI
                    {
                        lock (m_buttonSync)
                        {
                            l_result[i_byteIndex] = (byte)(m_buttonEvents >> 8);
                            l_buttonEventsRead = true;
                        }
                    }
                    break;

                    case 4: // LEDS LO
                    {
                        lock (m_ledSync)
                        {
                            l_result[i_byteIndex] = (byte)m_ledValues;
                        }
                    }
                    break;

                    case 5: // LEDS HI
                    {
                        lock (m_ledSync)
                        {
                            l_result[i_byteIndex] = (byte)(m_ledValues >> 8);
                        }
                    }
                    break;

                    case 6: // switches LO
                    {
                        lock (m_switchSync)
                        {
                            l_result[i_byteIndex] = (byte)m_switchValues;
                        }
                    }
                    break;

                    case 7: // switches HI
                    {
                        lock (m_switchSync)
                        {
                            l_result[i_byteIndex] = (byte)(m_switchValues >> 8);
                        }
                    }
                    break;

                    default:
                    {
                        throw new Exception("Invalid register offset");
                    }
                }
            }

            lock (m_buttonSync)
            {
                if (l_buttonEventsRead)
                {
                    m_buttonEvents = 0x0000;
                }
            }

            return l_result;
        }

        void I_RVPeripheral.WriteRegisters(uint offset, byte[] data)
        {
            for (int i_byteIndex = 0; i_byteIndex < data.Length; ++i_byteIndex)
            {
                int l_registerOffset = (int)offset + i_byteIndex;

                switch (l_registerOffset)
                {
                    case 0: // segments LO
                    {
                        lock (m_segmentSync)
                        {
                            m_segmentValues &= 0xFF00;
                            m_segmentValues |= (UInt16)(data[i_byteIndex] & 0x7F);
                        }
                    }
                    break;

                    case 1: // segments HI
                    {
                        lock (m_segmentSync)
                        {
                            m_segmentValues &= 0x00FF;
                            m_segmentValues |= (UInt16)(((UInt16)(data[i_byteIndex] & 0x7F)) << 8);
                        }
                    }
                    break;

                    case 2: // buttons LO
                    {
                        lock (m_buttonSync)
                        {
                            m_buttonInterruptMask &= 0xFF00;
                            m_buttonInterruptMask |= (UInt16)data[i_byteIndex];
                        }
                    }
                    break;

                    case 3: // buttons HI
                    {
                        lock (m_buttonSync)
                        {
                            m_buttonInterruptMask &= 0x00FF;
                            m_buttonInterruptMask |= (UInt16)(((UInt16)data[i_byteIndex]) << 8);
                        }
                    }
                    break;

                    case 4: // LEDS LO
                    {
                        lock (m_ledSync)
                        {
                            m_ledValues &= 0xFF00;
                            m_ledValues |= (UInt16)data[i_byteIndex];
                        }
                    }
                    break;

                    case 5: // LEDS HI
                    {
                        lock (m_ledSync)
                        {
                            m_ledValues &= 0x00FF;
                            m_ledValues |= (UInt16)(((UInt16)data[i_byteIndex]) << 8);
                        }
                    }
                    break;

                    case 6: // switches LO
                    case 7: // switches HI
                        break; // ignore writes to theese registers

                    default:
                    {
                        throw new Exception("Invalid register offset");
                    }
                }
            }
        }

        public UInt16 GetSegmentValues()
        {
            UInt16 l_values;
            lock (m_segmentSync)
            {
                l_values = m_segmentValues;
            }

            return l_values;
        }

        public UInt16 GetLEDValues()
        {
            UInt16 l_values;
            lock (m_ledSync)
            {
                l_values = m_ledValues;
            }

            return l_values;
        }

        public void RegisterButtonPressed(int buttonIndex)
        {
            if (buttonIndex < 0 || buttonIndex >= 16)
            {
                throw new Exception("I fucked up!");
            }

            UInt16 l_buttonBit = (UInt16)(1 << buttonIndex);

            bool l_triggerInterrupt = false;
            lock (m_buttonSync)
            {
                m_buttonEvents |= l_buttonBit;
                if ((m_buttonInterruptMask & l_buttonBit) != 0)
                {
                    l_triggerInterrupt = true;
                }
            }

            if (l_triggerInterrupt)
            {
                m_emulator.QueueExternalInterrupt(RVVector.External8, 0);
            }
        }

        public void SwitchStateChanged(int switchIndex, bool active)
        {
            if (switchIndex < 0 || switchIndex >= 16)
            {
                throw new Exception("I fucked up!");
            }

            UInt16 l_switchBit = (UInt16)(1 << switchIndex);

            lock (m_switchSync)
            {
                m_switchValues = active ? (UInt16)(m_switchValues | l_switchBit) : (UInt16)(m_switchValues & ~l_switchBit);
            }
        }
    }
}
