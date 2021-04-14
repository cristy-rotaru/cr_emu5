﻿using System;
using System.Collections.Generic;

namespace Emu5
{
    public class RVMemoryException : Exception
    {
        private readonly UInt32 m_faultingAddress;

        public RVMemoryException(String message, UInt32 faultingAddress) : base(message)
        {
            m_faultingAddress = faultingAddress;
        }

        public UInt32 FaultingAddress
        {
            get
            {
                return m_faultingAddress;
            }
        }
    }

    public class RVMemoryMap
    {
        Dictionary<UInt32, UInt64> m_memoryDictionary; // mapped to 8 bytes to reduce memory consumption
        List<Interval> m_memoryRanges;

        UInt64 m_uninitializedValue;

        IOPanelWindow m_IOPanelPeripheral;

        const UInt32 c_IOPanelBaseAddress = 0x00000110;
        const int c_IOPanelSpaceSize = 8;

        public RVMemoryMap()
        {
            m_memoryDictionary = new Dictionary<UInt32, UInt64>();

            m_memoryRanges = new List<Interval>();
            m_memoryRanges.Add(new Interval { start = 0x00000000, end = 0x9FFFFFFF }); // default ranges
            m_memoryRanges.Add(new Interval { start = 0xC0000000, end = 0xFFFFFFFF }); // will be replaced in the future

            m_uninitializedValue = 0xFFFFFFFFFFFFFFFF;
        }

        public byte UninitializedMemoryValue
        {
            set
            {
                UInt64 l_val = (UInt64)value;

                m_uninitializedValue = l_val;
                m_uninitializedValue |= l_val << 8;
                m_uninitializedValue |= l_val << 16;
                m_uninitializedValue |= l_val << 24;
                m_uninitializedValue |= l_val << 32;
                m_uninitializedValue |= l_val << 40;
                m_uninitializedValue |= l_val << 48;
                m_uninitializedValue |= l_val << 56;
            }
        }

        public void RegisterIOPanelPeripheral(IOPanelWindow ioPanel)
        {
            m_IOPanelPeripheral = ioPanel;
        }

        public byte?[] Read(UInt32 startAddress, int count)
        {
            byte?[] l_toReturn = new byte?[count];

            UInt32? l_currentAddress = null; // implemented to eliminate redundant reads
            UInt64 l_currentData = 0;

            for (int i_index = 0; i_index < count; ++i_index)
            {
                UInt32 l_byteAddress = startAddress + (UInt32)i_index;

                if (IsInRange(l_byteAddress))
                {
                    if (l_byteAddress >= c_IOPanelBaseAddress && l_byteAddress < (UInt32)(c_IOPanelBaseAddress + c_IOPanelSpaceSize))
                    {
                        int l_bytesLeftToRead = count - i_index;
                        int l_bytesFromIO = l_bytesLeftToRead > c_IOPanelSpaceSize ? c_IOPanelSpaceSize : l_bytesLeftToRead;

                        byte[] l_ioData = m_IOPanelPeripheral.ReadRegisters(l_byteAddress - c_IOPanelBaseAddress, l_bytesFromIO);
                        for (int i_ioByteIndex = 0; i_ioByteIndex < l_bytesFromIO; ++i_ioByteIndex)
                        {
                            l_toReturn[i_index + i_ioByteIndex] = l_ioData[i_ioByteIndex];
                        }

                        i_index += l_bytesFromIO - 1;
                    }
                    else
                    {
                        UInt32 l_dwordAddress = l_byteAddress & ~(UInt32)0x7;
                        int l_byteIndex = (int)l_byteAddress & 0x7;

                        if (l_currentAddress != l_dwordAddress)
                        { // will perform a new read
                            l_currentAddress = l_dwordAddress;

                            if (m_memoryDictionary.ContainsKey(l_dwordAddress))
                            {
                                l_currentData = m_memoryDictionary[l_dwordAddress];
                            }
                            else
                            {
                                l_currentData = m_uninitializedValue;
                            }
                        }

                        l_toReturn[i_index] = (byte)(l_currentData >> (l_byteIndex * 8));
                    }
                }
                else
                {
                    l_toReturn[i_index] = null;
                }
            }

            return l_toReturn;
        }

        public byte?[] ReadIgnoreIO(UInt32 startAddress, int count)
        {
            byte?[] l_toReturn = new byte?[count];

            UInt32? l_currentAddress = null; // implemented to eliminate redundant reads
            UInt64 l_currentData = 0;

            for (int i_index = 0; i_index < count; ++i_index)
            {
                UInt32 l_byteAddress = startAddress + (UInt32)i_index;

                if (IsInRange(l_byteAddress))
                {
                    if (l_byteAddress >= c_IOPanelBaseAddress && l_byteAddress < (UInt32)(c_IOPanelBaseAddress + c_IOPanelSpaceSize))
                    {
                        l_toReturn[i_index] = null; // reads performed with this function should not trigger peripherals
                    }
                    else
                    {
                        UInt32 l_dwordAddress = l_byteAddress & ~(UInt32)0x7;
                        int l_byteIndex = (int)l_byteAddress & 0x7;

                        if (l_currentAddress != l_dwordAddress)
                        { // will perform a new read
                            l_currentAddress = l_dwordAddress;

                            if (m_memoryDictionary.ContainsKey(l_dwordAddress))
                            {
                                l_currentData = m_memoryDictionary[l_dwordAddress];
                            }
                            else
                            {
                                l_currentData = m_uninitializedValue;
                            }
                        }

                        l_toReturn[i_index] = (byte)(l_currentData >> (l_byteIndex * 8));
                    }
                }
                else
                {
                    l_toReturn[i_index] = null;
                }
            }

            return l_toReturn;
        }

        public void Write(UInt32 startAddress, byte[] data)
        {
            UInt32? l_currentAddress = null; // implemented to eliminate redundant reads
            UInt64 l_currentData = 0;

            for (int i_index = 0; i_index < data.Length; ++i_index)
            {
                UInt32 l_byteAddress = startAddress + (UInt32)i_index;

                if (IsInRange(l_byteAddress) == false)
                {
                    throw new RVMemoryException("Attempting to write reserved memory address.", l_byteAddress);
                }

                if (l_byteAddress >= c_IOPanelBaseAddress && l_byteAddress < (UInt32)(c_IOPanelBaseAddress + c_IOPanelSpaceSize))
                {
                    int l_bytesLeftToWrite = data.Length - i_index;
                    int l_bytesToIO = l_bytesLeftToWrite > c_IOPanelSpaceSize ? c_IOPanelSpaceSize : l_bytesLeftToWrite;

                    byte[] l_ioData = new byte[l_bytesToIO];
                    for (int i_ioByteIndex = 0; i_ioByteIndex < l_bytesToIO; ++i_ioByteIndex)
                    {
                        l_ioData[i_ioByteIndex] = data[i_index + i_ioByteIndex];
                    }
                    m_IOPanelPeripheral.WriteRegisters(l_byteAddress - c_IOPanelBaseAddress, l_ioData);

                    i_index += l_bytesToIO - 1;
                }
                else
                {
                    UInt32 l_dwordAddress = l_byteAddress & ~(UInt32)0x7;
                    int l_byteIndex = (int)l_byteAddress & 0x7;

                    if (l_currentAddress != l_dwordAddress)
                    { // will perform a read
                        if (l_currentAddress != null)
                        { // commit what was calculated until now
                            m_memoryDictionary[(UInt32)l_currentAddress] = l_currentData;
                        }

                        l_currentAddress = l_dwordAddress;

                        if (m_memoryDictionary.ContainsKey(l_dwordAddress))
                        {
                            l_currentData = m_memoryDictionary[l_dwordAddress];
                        }
                        else
                        {
                            l_currentData = m_uninitializedValue;
                        }
                    }

                    UInt64 l_mask = ~((UInt64)0xFF << (l_byteIndex * 8));

                    l_currentData &= l_mask;
                    l_currentData |= (UInt64)data[i_index] << (l_byteIndex * 8);
                }
            }

            if (l_currentAddress != null)
            {
                m_memoryDictionary[(UInt32)l_currentAddress] = l_currentData;
            }
        }

        private bool IsInRange(UInt32 address)
        {
            foreach (Interval i_interval in m_memoryRanges)
            {
                if (address >= i_interval.start && address <= i_interval.end)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
