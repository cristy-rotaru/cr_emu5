﻿using System;
using System.Collections.Generic;

namespace Emu5
{
    public class RVEmulationException : Exception
    {
        public RVEmulationException(String message) : base(message) { }
    }

    public enum RVVector
    {
        Reset = 0,
        NMI,
        ECALL,
        MisalignedPC,
        MisalignedMemory,
        UndefinedMemory,
        InvalidInstruction,
        DivisionBy0,
        External8,
        External9,
        External10,
        External11,
        External12,
        External13,
        External14,
        External15,
        External16,
        External17,
        External18,
        External19,
        External20,
        External21,
        External22,
        External23,
        External24,
        External25,
        External26,
        External27,
        External28,
        External29,
        External30,
        External31
    }

    public class RVEmulator
    {
        bool m_halted;
        String m_haltReason;

        RVMemoryMap m_memoryMap;
        UInt32[] m_registerFile;

        UInt32 m_programCounter = 0x0;
        RVVector? m_trapHandled = null;
        bool m_interruptsEnabled = false;
        bool m_waitForInterrupt = false;
        bool[] m_pendingInterrupts;
        List<Tuple<RVVector, ushort>> m_queuedInterrupts;

        List<UInt32> m_breakpoints;
        bool m_ebreakExecuted;
        bool m_softReset;

        LogPerspective m_logger, m_loggerBackup;
        Verbosity m_loggingVerbosity;
        bool m_disableEcallLogging;

        public bool Halted
        {
            get
            {
                return m_halted;
            }

            set
            {
                m_halted = value;
                m_haltReason = "";
            }
        }

        public String HaltReason
        {
            get
            {
                return m_haltReason;
            }
        }

        public bool DisableEcallLogging
        {
            set
            {
                m_disableEcallLogging = value;
            }
        }

        public RVEmulator()
        {
            m_halted = true;
            m_haltReason = "";

            m_memoryMap = new RVMemoryMap();
            m_registerFile = new UInt32[32];
            m_pendingInterrupts = new bool[32];
            m_queuedInterrupts = new List<Tuple<RVVector, ushort>>();

            m_breakpoints = new List<UInt32>();
            m_ebreakExecuted = false;
            m_softReset = false;

            m_logger = null;
            m_loggerBackup = null;
            m_loggingVerbosity = Verbosity.Normal;
            m_disableEcallLogging = false;
        }

        public void RegisterLogger(LogPerspective logger)
        {
            m_logger = logger;
        }

        public void SetLoggingVerbosity(Verbosity level)
        {
            m_loggingVerbosity = level;
        }

        public void Assemble(String code, RVLabelReferenceMap labelMap, Dictionary<UInt32, String> pseudoInstructions, bool useIntegratedEcallHandler, UInt32 ecallBase = 0xFFFFF000)
        {
            RVAssembler.Assemble(code, m_memoryMap, labelMap, pseudoInstructions, useIntegratedEcallHandler, ecallBase);
            m_halted = false;
        }

        public void AddBreakpoint(UInt32 address)
        {
            lock (m_breakpoints)
            {
                if (m_breakpoints.Contains(address) == false)
                {
                    m_breakpoints.Add(address);
                }
            }
        }

        public void RemoveBreakpoint(UInt32 address)
        {
            lock (m_breakpoints)
            {
                m_breakpoints.Remove(address);
            }
        }

        public bool HasBreakpoint(UInt32 address)
        {
            bool l_result;

            lock (m_breakpoints)
            {
                l_result = m_breakpoints.Contains(address);
            }

            return l_result;
        }

        public bool BreakpointHit()
        {
            if (m_ebreakExecuted)
            {
                return true;
            }

            bool l_result;

            lock (m_breakpoints)
            {
                l_result = m_breakpoints.Contains(m_programCounter);
            }

            return l_result;
        }

        public RVVector? AboutToTakeInterrupt()
        {
            if (m_trapHandled != null)
            {
                return null;
            }

            bool[] l_nextInterruptVector = new bool[32];
            m_pendingInterrupts.CopyTo(l_nextInterruptVector, 0);

            foreach (Tuple<RVVector, ushort> i_pendingInterrupt in m_queuedInterrupts)
            {
                if (i_pendingInterrupt.Item2 == 1)
                {
                    l_nextInterruptVector[(int)i_pendingInterrupt.Item1] = true;
                }
            }

            for (int i_vectorIndex = 0; i_vectorIndex < 32; ++i_vectorIndex)
            {
                if (l_nextInterruptVector[i_vectorIndex])
                {
                    if (i_vectorIndex <= 1 || m_interruptsEnabled) // return only if interrupt can actually be taken by the core in it's current state
                    {
                        return (RVVector)i_vectorIndex;
                    }
                }
            }

            return null;
        }

        public RVVector? HandlingTrap()
        {
            return m_trapHandled;
        }

        public bool WaitingForInterrupt()
        {
            return m_waitForInterrupt;
        }

        public void QueueExternalInterrupt(RVVector vector, ushort deliveryTimeout)
        {
            if (vector > RVVector.NMI && vector < RVVector.External8)
            {
                throw new Exception("Invalid external interrupt");
            }

            if (deliveryTimeout == 0)
            {
                if (vector == RVVector.Reset)
                {
                    lock (m_queuedInterrupts)
                    {
                        m_queuedInterrupts.Add(new Tuple<RVVector, ushort>(vector, 1));
                    }
                }
                else
                {
                    lock (m_pendingInterrupts)
                    {
                        m_pendingInterrupts[(int)vector] = true;
                    }
                }
            }
            else
            {
                lock (m_queuedInterrupts)
                {
                    m_queuedInterrupts.Add(new Tuple<RVVector, ushort>(vector, (ushort)(deliveryTimeout + 1)));
                }
            }
        }

        public void ResetProcessor()
        {
            m_logger?.LogText("Processor reset", true);

            for (int i_index = 0; i_index < 32; ++i_index)
            {
                m_registerFile[i_index] = 0x0;
                m_pendingInterrupts[i_index] = false;

                if (m_loggingVerbosity > Verbosity.Minimal)
                {
                    m_logger?.LogText(String.Format("x{0,-2} <= 0x{1,8:X8}", i_index, 0), true);
                }
            }

            m_trapHandled = null;
            m_interruptsEnabled = false;
            m_waitForInterrupt = false;
            m_programCounter = 0x0;

            byte?[] l_initialProgramCounter = m_memoryMap.Read(0x0, 4);

            if (m_loggingVerbosity == Verbosity.Detailed)
            {
                m_logger?.LogText("Memory read:", false);
                m_logger?.LogByteArray(l_initialProgramCounter, false);
                m_logger?.LogText(String.Format("<= [0x{0,8:X8}] (Reset vector)", 0), true);
            }

            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
            {
                if (l_initialProgramCounter[i_byteIndex] == null)
                {
                    throw new RVEmulationException("Vector table memory range is not properly defined.");
                }
                m_programCounter <<= 8;
                m_programCounter |= (UInt32)l_initialProgramCounter[i_byteIndex];
            }

            m_logger?.LogText(String.Format("Register write: PC <= 0x{0,8:X8}", m_programCounter), true);

            m_memoryMap.ResetAllPeripherals();
        }

        public void SingleStep()
        {
            if (m_halted)
            {
                return;
            }

            m_ebreakExecuted = false;

            bool l_mustReset = false;

            lock (m_queuedInterrupts)
            {
                for (int i_interruptIndex = m_queuedInterrupts.Count - 1; i_interruptIndex >= 0; --i_interruptIndex)
                {
                    Tuple<RVVector, ushort> l_interrupt = m_queuedInterrupts[i_interruptIndex];
                    ushort l_newTimeout = (ushort)(l_interrupt.Item2 - 1);

                    if (l_newTimeout == 0)
                    {
                        m_queuedInterrupts.RemoveAt(i_interruptIndex);

                        if (l_interrupt.Item1 == RVVector.Reset)
                        {
                            l_mustReset = true;
                            continue;
                        }

                        if (m_interruptsEnabled || l_interrupt.Item1 == RVVector.NMI)
                        {
                            lock (m_pendingInterrupts)
                            {
                                m_pendingInterrupts[(int)l_interrupt.Item1] = true;
                            }
                        }
                    }
                    else
                    {
                        m_queuedInterrupts[i_interruptIndex] = new Tuple<RVVector, ushort>(l_interrupt.Item1, l_newTimeout);
                    }
                }
            }

            if (l_mustReset)
            {
                ResetProcessor();
                return;
            }

            if (m_trapHandled == null)
            {
                lock (m_pendingInterrupts)
                {
                    if (m_pendingInterrupts[1])
                    {
                        m_logger?.LogText("Taking interrupt: NMI", true);

                        m_pendingInterrupts[1] = false;
                        LoadVector(RVVector.NMI, CreateByteStream(m_programCounter)); // NMI can not be masked
                        m_waitForInterrupt = false;

                        m_logger?.LogText(String.Format("Register write: PC <= 0x{0,8:X8}", m_programCounter), true);

                        return;
                    }

                    bool l_alreadyLogged = false;

                    for (int i_vectorIndex = 8; i_vectorIndex < 32; ++i_vectorIndex)
                    {
                        if (m_pendingInterrupts[i_vectorIndex])
                        {
                            m_pendingInterrupts[i_vectorIndex] = false;

                            if (m_interruptsEnabled)
                            {
                                m_logger?.LogText(String.Format("Taking interrupt: External{0}", i_vectorIndex), true);

                                LoadVector((RVVector)i_vectorIndex, CreateByteStream(m_programCounter));
                                m_waitForInterrupt = false;

                                m_logger?.LogText(String.Format("Register write: PC <= 0x{0,8:X8}", m_programCounter), true);

                                return;
                            }
                            else
                            {
                                if (l_alreadyLogged == false)
                                {
                                    l_alreadyLogged = true;
                                    m_logger?.LogText(String.Format("Ignoring external{0} because interrupts are disabled", i_vectorIndex), true);
                                }
                            }
                        }
                    }

                }
            }

            if (m_waitForInterrupt)
            {
                m_logger?.LogText("Waiting for interrupt", true);
                return;
            }

            if ((m_programCounter & 0x3) != 0)
            {
                m_logger?.LogText("Taking trap: MisalignedPC (PC is not aligned to 32 bits)", true);

                LoadVector(RVVector.MisalignedPC, CreateByteStream(m_programCounter)); // misaligned PC fault

                m_logger?.LogText(String.Format("Register write: PC <= 0x{0,8:X8}", m_programCounter), true);

                return;
            }

            byte?[] l_rawData = m_memoryMap.Read(m_programCounter, 4);

            if (m_loggingVerbosity > Verbosity.Minimal)
            {
                m_logger?.LogText("Code read:", false);
                m_logger?.LogByteArray(l_rawData, false);
                m_logger?.LogText(String.Format("<= [0x{0, 8:X8}]", m_programCounter), true);
            }

            UInt32 l_instructionData = 0x0;
            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
            {
                if (l_rawData[i_byteIndex] == null)
                {
                    LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, m_programCounter)); // undefined memory address fault
                    return;
                }

                l_instructionData <<= 8;
                l_instructionData |= (UInt32)l_rawData[i_byteIndex];
            }

            Tuple<String, String> l_dissassembly = RVInstructions.DisassembleIntruction(l_instructionData);
            if (String.IsNullOrEmpty(l_dissassembly.Item1))
            {
                m_logger?.LogText(String.Format("Decode error: 0x{0,8:X8} is not a valid instruction", l_instructionData), true);
                m_logger?.LogText("Taking trap: InvalidInstruction (Invalid instruction code)", true);

                LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter));
            }
            else
            {
                m_logger?.LogText(String.Format("Decoded: {0}", l_dissassembly.Item1), false);
                if (m_loggingVerbosity == Verbosity.Minimal)
                {
                    m_logger?.NewLine();
                }
                else if (m_loggingVerbosity == Verbosity.Normal)
                {
                    m_logger?.LogText(String.Format("({0})", l_dissassembly.Item2), true);
                }
                else
                {
                    m_logger?.LogText(String.Format("({0} | 0x{1,8:X8})", l_dissassembly.Item2, l_instructionData), true);
                }

                DecodeAndExecute(l_instructionData);
            }

            if (m_halted == false && m_softReset == false)
            {
                m_logger?.LogText(String.Format("Register write: PC <= 0x{0,8:X8}", m_programCounter), true);
            }

            m_softReset = false;
        }

        public UInt32 GetProgramCounter()
        {
            return m_programCounter;
        }

        public UInt32[] GetRegisterFile()
        {
            return m_registerFile;
        }

        public RVMemoryMap GetMemoryMapReference()
        {
            return m_memoryMap;
        }

        private byte[] CreateByteStream(params UInt32[] data)
        {
            byte[] l_toReturn = new byte[data.Length * 4];

            for (int i_index = 0; i_index < data.Length; ++i_index)
            {
                l_toReturn[4 * i_index] = (byte)(data[i_index] & 0xFF);
                l_toReturn[4 * i_index + 1] = (byte)((data[i_index] >> 8) & 0xFF);
                l_toReturn[4 * i_index + 2] = (byte)((data[i_index] >> 16) & 0xFF);
                l_toReturn[4 * i_index + 3] = (byte)((data[i_index] >> 24) & 0xFF);
            }

            return l_toReturn;
        }

        private void LoadVector(RVVector vectorIndex, byte[] contextInfo)
        {
            if (m_trapHandled != null)
            {
                m_logger?.LogText("Fault during exception/interrupt handling", true);
                m_logger?.LogText("Halting core", true);

                m_halted = true;
                m_haltReason = "Fault during exception/interrupt handling";
                return;
            }

            m_memoryMap.Write(0x100, contextInfo); // save trap info

            if (m_loggingVerbosity == Verbosity.Detailed)
            {
                m_logger?.LogText(String.Format("Memory write: [0x{0,8:X8}] <=", 0x100), false);
                m_logger?.LogByteArray(contextInfo, false);
                m_logger?.LogText("(Context info)", true);
            }

            byte[] l_registerBackup = CreateByteStream(m_registerFile);
            m_memoryMap.Write(0x80, l_registerBackup); // backup registers

            if (m_loggingVerbosity == Verbosity.Detailed)
            {
                m_logger?.LogText(String.Format("Memory write: [0x{0,8:X8}] <=", 0x80), false);
                m_logger?.LogByteArray(l_registerBackup, false);
                m_logger?.LogText("(Saved registers)", true);
            }

            // load corresponding PC
            UInt32 l_vectorAddress = 0x4 * (UInt32)vectorIndex;
            byte?[] l_vector = m_memoryMap.Read(l_vectorAddress, 4);

            if (m_loggingVerbosity > Verbosity.Minimal)
            {
                m_logger?.LogText("Memory read:", false);
                m_logger?.LogByteArray(l_vector, false);
                m_logger?.LogText(String.Format("<= [0x{0,8:X8}] (Vector)", l_vectorAddress), true);
            }

            m_programCounter = 0x0;
            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
            {
                if (l_vector[i_byteIndex] == null)
                {
                    throw new RVEmulationException("Vector table memory range is not properly defined.");
                }
                m_programCounter <<= 8;
                m_programCounter |= (UInt32)l_vector[i_byteIndex];
            }

            /*
             vectors:
             * 0x00 = Reset
             * 0x04 = NMI | saves PC
             * 0x08 = ECALL | saves PC+4
             * 0x0C = Misaligned PC fault | saves PC
             * 0x10 = Misaligned memory access fault | saves PC, memory address
             * 0x14 = Access to undefined memory space fault | saves PC, memory address
             * 0x18 = Invalid instruction fault | saves PC
             * 0x1C = Division by 0 or division overflow fault | saves PC
             * 0x20-0x7C - programmable and external interrupts | saves PC
            */

            m_trapHandled = vectorIndex;
        }

        private void WriteRegister(byte index, UInt32 data)
        {
            if (index > 31)
            {
                throw new RVEmulationException("Register index out of range."); // if this happens I fucked something up really bad
            }

            if (m_loggingVerbosity == Verbosity.Detailed)
            {
                m_logger?.LogText(String.Format("Register write: x{0} --0x{1,8:X8}-- <= 0x{2,8:X8}", index, m_registerFile[index], data), false);
            }
            else
            {
                m_logger?.LogText(String.Format("Register write: x{0} <= 0x{1,8:X8}", index, data), false);
            }

            if (index != 0)
            {
                m_logger?.NewLine();
                m_registerFile[index] = data;
            }
            else
            {
                if (m_loggingVerbosity > Verbosity.Minimal)
                {
                    m_logger?.LogText("(Write to x0 suppressed)", true);
                }
                else
                {
                    m_logger?.NewLine();
                }
            }
        }

        private UInt32 ReadRegister(byte index)
        {
            if (index > 31)
            {
                throw new RVEmulationException("Register index out of range."); // if this happens I fucked something up really bad
            }

            UInt32 l_registerValue = m_registerFile[index];

            if (m_loggingVerbosity == Verbosity.Detailed)
            {
                m_logger?.LogText(String.Format("Register read: 0x{0,8:X8} <= x{1}", l_registerValue, index), true);
            }

            return l_registerValue;
        }

        private bool StoreToMemory(byte opType, UInt32 address, UInt32 data)
        {
            switch (opType)
            {
                case 0b000: // SB
                {
                    byte[] l_writeData = new byte[1];
                    l_writeData[0] = (byte)(data & 0xFF);
                    try
                    {
                        m_memoryMap.Write(address, l_writeData);
                    }
                    catch (RVMemoryException)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    m_logger?.LogText(String.Format("Memory write: [0x{0,8:X8}] <=", address), false);
                    m_logger?.LogByteArray(l_writeData, true);

                    return true;
                }

                case 0b001: // SH
                {
                    if ((address & 0x1) != 0)
                    {
                        m_logger?.LogText("Taking trap: MisalignedMemoryAddress (The store address is not aligned to the data size)", true);

                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte[] l_writeData = new byte[2];
                    l_writeData[0] = (byte)(data & 0xFF);
                    l_writeData[1] = (byte)((data >> 8) & 0xFF);
                    try
                    {
                        m_memoryMap.Write(address, l_writeData);
                    }
                    catch (RVMemoryException)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    m_logger?.LogText(String.Format("Memory write: [0x{0,8:X8}] <=", address), false);
                    m_logger?.LogByteArray(l_writeData, true);

                    return true;
                }

                case 0b010: // SW
                {
                    if ((address & 0x3) != 0)
                    {
                        m_logger?.LogText("Taking trap: MisalignedMemoryAddress (The store address is not aligned to the data size)", true);

                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte[] l_writeData = new byte[4];
                    l_writeData[0] = (byte)(data & 0xFF);
                    l_writeData[1] = (byte)((data >> 8) & 0xFF);
                    l_writeData[2] = (byte)((data >> 16) & 0xFF);
                    l_writeData[3] = (byte)((data >> 24) & 0xFF);
                    try
                    {
                        m_memoryMap.Write(address, l_writeData);
                    }
                    catch (RVMemoryException)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    m_logger?.LogText(String.Format("Memory write: [0x{0,8:X8}] <=", address), false);
                    m_logger?.LogByteArray(l_writeData, true);

                    return true;
                }

                default:
                {
                    LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                    return false;
                }
            }
        }

        private bool LoadFromMemory(byte opType, byte destination, UInt32 address)
        {
            UInt32 l_readValue;

            switch (opType)
            {
                case 0b000: // LB
                {
                    byte?[] l_rawData = m_memoryMap.Read(address, 1);

                    m_logger?.LogText("Memory read:", false);
                    m_logger?.LogByteArray(l_rawData, false);
                    m_logger?.LogText(String.Format("<= [0x{0,8:X8}]", address), true);

                    if (l_rawData[0] == null)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0];
                    l_readValue = (UInt32)(((Int32)l_readValue << 24) >> 24); // sign extend

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} (sign extended)", l_readValue), true);
                    }
                }
                break;

                case 0b001: // LH
                {
                    if ((address & 0x1) != 0)
                    {
                        m_logger?.LogText("Taking trap: MisalignedMemoryAddress (The load address is not aligned to the data size)", true);

                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 2);

                    m_logger?.LogText("Memory read:", false);
                    m_logger?.LogByteArray(l_rawData, false);
                    m_logger?.LogText(String.Format("<= [0x{0,8:X8}]", address), true);

                    if (l_rawData[0] == null || l_rawData[1] == null)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0] | ((UInt32)l_rawData[1] << 8);
                    l_readValue = (UInt32)(((Int32)l_readValue << 16) >> 16); // sign extend

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} (sign extended)", l_readValue), true);
                    }
                }
                break;

                case 0b010: // LW
                {
                    if ((address & 0x3) != 0)
                    {
                        m_logger?.LogText("Taking trap: MisalignedMemoryAddress (The load address is not aligned to the data size)", true);

                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 4);

                    m_logger?.LogText("Memory read:", false);
                    m_logger?.LogByteArray(l_rawData, false);
                    m_logger?.LogText(String.Format("<= [0x{0,8:X8}]", address), true);

                    l_readValue = 0x0;
                    for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
                    {
                        if (l_rawData[i_byteIndex] == null)
                        {
                            m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                            LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                            return false;
                        }
                        l_readValue <<= 8;
                        l_readValue |= (UInt32)l_rawData[i_byteIndex];
                    }
                }
                break;

                case 0b100: // LBU
                {
                    byte?[] l_rawData = m_memoryMap.Read(address, 1);

                    m_logger?.LogText("Memory read:", false);
                    m_logger?.LogByteArray(l_rawData, false);
                    m_logger?.LogText(String.Format("<= [0x{0,8:X8}]", address), true);

                    if (l_rawData[0] == null)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0];
                }
                break;

                case 0b101: // LHU
                {
                    if ((address & 0x1) != 0)
                    {
                        m_logger?.LogText("Taking trap: MisalignedMemoryAddress (The load address is not aligned to the data size)", true);

                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 2);

                    m_logger?.LogText("Memory read:", false);
                    m_logger?.LogByteArray(l_rawData, false);
                    m_logger?.LogText(String.Format("<= [0x{0,8:X8}]", address), true);

                    if (l_rawData[0] == null || l_rawData[1] == null)
                    {
                        m_logger?.LogText("Taking trap: UndefinedAddressSpace (The memory address is not defined)", true);

                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0] | ((UInt32)l_rawData[1] << 8);
                }
                break;

                default:
                {
                    LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                    return false;
                }
            }

            WriteRegister(destination, l_readValue);
            return true;
        }

        private bool ExecuteALU(UInt16 opType, byte operand1, UInt32 operand2, byte destination, bool imm)
        {
            /* To allow accurate logging this function will behave differently for I-type and R-type instructions
             * aperand1 is always a register index
             * operand2 is a register index if imm==false and 32-bit immediate if imm==true
             */

            UInt32 l_data1 = ReadRegister(operand1);
            UInt32 l_data2 = imm ? operand2 : ReadRegister((byte)operand2);

            switch (opType)
            {
                case 0b0000000000: // ADD
                {
                    UInt32 l_result = l_data1 + l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} + imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} + x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0100000000: // SUB
                {
                    UInt32 l_result = l_data1 - l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} - 0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} - imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} - x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000000001: // SLL
                {
                    UInt32 l_result = l_data1 << (int)(l_data2 & 0x1F);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} << 0x{1,2:X2} = 0x{2,8:X8}", l_data1, l_data2 & 0x1F, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} << imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} << x{1}[4:0])", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000000010: // SLT
                {
                    bool l_result = (Int32)l_data1 < (Int32)l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} < (int)0x{1,8:X8} = {2}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((int)x{0} < (int)imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((int)x{0} < (int)x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result ? (UInt32)1 : (UInt32)0);
                }
                break;

                case 0b0000000011: // SLTU
                {
                    bool l_result = l_data1 < l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (uint)0x{0,8:X8} < (uint)0x{1,8:X8} = {2}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} < (uint)imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} < (uint)x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result ? (UInt32)1 : (UInt32)0);
                }
                break;

                case 0b0000000100: // XOR
                {
                    UInt32 l_result = l_data1 ^ l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} ^ 0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} ^ imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} ^ x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000000101: // SRL
                {
                    UInt32 l_result = l_data1 >> (int)(l_data2 & 0x1F);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} >> 0x{1,2:X2} = 0x{2,8:X8}", l_data1, l_data2 & 0x1F, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} >> imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} >> x{1}[4:0])", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0100000101: // SRA
                {
                    UInt32 l_result = (UInt32)((Int32)l_data1 >> (int)(l_data2 & 0x1F));

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} >>> 0x{1,2:X2} = 0x{2,8:X8}", l_data1, l_data2 & 0x1F, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} >>> imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} >>> x{1}[4:0])", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000000110: // OR
                {
                    UInt32 l_result = l_data1 | l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} | 0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} | imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} | x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000000111: // AND
                {
                    UInt32 l_result = l_data1 & l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} & 0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} & imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} & x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001000: // MUL
                {
                    UInt32 l_result = l_data1 * l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} * 0x{1,8:X8} & 0xFFFFFFFF = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("(x{0} * imm & 0xFFFFFFFF)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("(x{0} * x{1} & 0xFFFFFFFF)", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001001: // MULH
                {
                    Int64 l_extendedData1 = (Int64)l_data1;
                    Int64 l_extendedData2 = (Int64)l_data2;

                    // sign extend
                    l_extendedData1 <<= 32;
                    l_extendedData2 <<= 32;
                    l_extendedData1 >>= 32;
                    l_extendedData2 >>= 32;

                    UInt32 l_result = (UInt32)((l_extendedData1 * l_extendedData2) >> 32);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} * (int)0x{1,8:X8} >> 32 = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((int)x{0} * (int)imm >> 32)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((int)x{0} * (int)x{1} >> 32)", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001010: // MULHSU
                {
                    Int64 l_extendedData1 = (Int64)l_data1;
                    Int64 l_extendedData2 = (Int64)l_data2;

                    // sign extend
                    l_extendedData1 <<= 32;
                    l_extendedData1 >>= 32;

                    l_extendedData2 &= 0xFFFFFFFF;

                    UInt32 l_result = (UInt32)((l_extendedData1 * l_extendedData2) >> 32);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} * (uint)0x{1,8:X8} >> 32 = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((int)x{0} * (uint)imm >> 32)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((int)x{0} * (uint)x{1} >> 32)", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001011: // MULHU
                {
                    UInt32 l_result = (UInt32)(((UInt64)l_data1 * (UInt64)l_data2) >> 32);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (uint)0x{0,8:X8} * (uint)0x{1,8:X8} >> 32 = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} * (uint)imm >> 32)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} * (uint)x{1} >> 32)", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001100: // DIV
                {
                    if (l_data2 == 0)
                    {
                        m_logger?.LogText("Taking trap: DivisionBy0 (Divider is 0)", true);

                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 fault
                        return false;
                    }

                    if (l_data1 == 0x80000000 && l_data2 == 0xFFFFFFFF)
                    {
                        m_logger?.LogText("Taking trap: DivisionBy0 (Division overflow)", true);

                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division overflow fault
                        return false;
                    }

                    UInt32 l_result = (UInt32)((Int32)l_data1 / (Int32)l_data2);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} / (int)0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((int)x{0} / (int)imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((int)x{0} / (int)x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001101: // DIVU
                {
                    if (l_data2 == 0)
                    {
                        m_logger?.LogText("Taking trap: DivisionBy0 (Divider is 0)", true);

                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    UInt32 l_result = l_data1 / l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (uint)0x{0,8:X8} / (uint)0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} / (uint)imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} / (uint)x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001110: // REM
                {
                    if (l_data2 == 0)
                    {
                        m_logger?.LogText("Taking trap: DivisionBy0 (Divider is 0)", true);

                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    UInt32 l_result = (UInt32)((Int32)l_data1 % (Int32)l_data2);

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} % (int)0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((int)x{0} % (int)imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((int)x{0} % (int)x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                case 0b0000001111: // REMU
                {
                    if (l_data2 == 0)
                    {
                        m_logger?.LogText("Taking trap: DivisionBy0 (Divider is 0)", true);

                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    UInt32 l_result = l_data1 % l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (uint)0x{0,8:X8} % (uint)0x{1,8:X8} = 0x{2,8:X8}", l_data1, l_data2, l_result), false);
                        if (imm)
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} % (uint)imm)", operand1), true);
                        }
                        else
                        {
                            m_logger?.LogText(String.Format("((uint)x{0} % (uint)x{1})", operand1, operand2), true);
                        }
                    }

                    WriteRegister(destination, l_result);
                }
                break;

                default:
                {
                    LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                    return false;
                }
            }

            return true;
        }

        private bool? MeetsBranchConditions(byte branchOp, byte register1, byte register2)
        { // returns null if branchOp is not valid
            UInt32 l_data1 = ReadRegister(register1);
            UInt32 l_data2 = ReadRegister(register2);

            switch (branchOp)
            {
                case 0b000: // BEQ
                {
                    bool l_result = l_data1 == l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} == 0x{1,8:X8} = {2} (x{3} == x{4})", l_data1, l_data2, l_result, register1, register2), true);
                    }

                    return l_result;
                }

                case 0b001: // BNE
                {
                    bool l_result = l_data1 != l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} != 0x{1,8:X8} = {2} (x{3} != x{4})", l_data1, l_data2, l_result, register1, register2), true);
                    }

                    return l_result;
                }

                case 0b100: // BLT
                {
                    bool l_result = (Int32)l_data1 < (Int32)l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} < (int)0x{1,8:X8} = {2} ((int)x{3} < (int)x{4})", l_data1, l_data2, l_result, register1, register2), true);
                    }

                    return l_result;
                }

                case 0b101: // BGE
                {
                    bool l_result = (Int32)l_data1 >= (Int32)l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (int)0x{0,8:X8} >= (int)0x{1,8:X8} = {2} ((int)x{3} >= (int)x{4})", l_data1, l_data2, l_result, register1, register2), true);
                    }

                    return l_result;
                }

                case 0b110: // BLTU
                {
                    bool l_result = l_data1 < l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (uint)0x{0,8:X8} < (uint)0x{1,8:X8} = {2} ((uint)x{3} < (uint)x{4})", l_data1, l_data2, l_result, register1, register2), true);
                    }

                    return l_result;
                }

                case 0b111: // BGEU
                {
                    bool l_result = l_data1 >= l_data2;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: (uint)0x{0,8:X8} >= (uint)0x{1,8:X8} = {2} ((uint)x{3} >= (uint)x{4})", l_data1, l_data2, l_result, register1, register2), true);
                    }

                    return l_result;
                }

                default:
                {
                    return null;
                }
            }
        }

        private bool ExecuteSystemInstruction(UInt16 opType)
        {// will return true if no vector has been triggered and false otherwise
            switch (opType)
            {
                case 0x000: // ecall
                {
                    if (m_disableEcallLogging && m_trapHandled == null)
                    {
                        m_loggerBackup = m_logger;
                        m_logger = null;
                    }

                    LoadVector(RVVector.ECALL, CreateByteStream(m_programCounter + 4));
                    return false;
                }

                case 0x001: // ebreak
                {
                    m_logger?.LogText("Forcing breakpoint", true);

                    m_ebreakExecuted = true;
                    return true;
                }

                case 0xFFF: // hlt
                {
                    if (m_trapHandled == RVVector.ECALL && m_disableEcallLogging)
                    {
                        m_logger = m_loggerBackup;
                    }

                    m_trapHandled = null;
                    m_logger?.LogText("Halting core", true);

                    m_halted = true;
                    m_haltReason = "Halt instruction executed";
                    return false;
                }

                case 0xFFE: // rst
                {
                    if (m_trapHandled == RVVector.ECALL && m_disableEcallLogging)
                    {
                        m_logger = m_loggerBackup;
                    }

                    m_softReset = true;
                    ResetProcessor();
                    return false;
                }

                case 0x107: // ien
                {
                    m_logger?.LogText("Enabling interrupts", true);

                    lock (m_pendingInterrupts)
                    {
                        m_interruptsEnabled = true;
                        for (int i_interruptIndex = 2; i_interruptIndex < 32; ++i_interruptIndex)
                        {
                            m_pendingInterrupts[i_interruptIndex] = false;
                        }
                    }

                    return true;
                }

                case 0x106: // idis
                {
                    m_logger?.LogText("Disabling interrupts", true);

                    lock (m_pendingInterrupts)
                    {
                        m_interruptsEnabled = false;
                        for (int i_interruptIndex = 2; i_interruptIndex < 32; ++i_interruptIndex)
                        {
                            m_pendingInterrupts[i_interruptIndex] = false;
                        }
                    }

                    return true;
                }

                case 0x105: // wfi
                {
                    if (m_trapHandled == null) // no effect if already in interrupt handler
                    {
                        m_logger?.LogText("Execution stopped until next interrupt", true);

                        m_waitForInterrupt = true;
                    }

                    return true;
                }

                case 0x102: // iret
                {
                    if (m_trapHandled != null)
                    {
                        if (m_trapHandled == RVVector.ECALL && m_disableEcallLogging)
                        {
                            m_logger = m_loggerBackup;
                        }

                        // restore registers
                        byte?[] l_registerBackup = m_memoryMap.Read(0x80, 128);

                        if (m_loggingVerbosity == Verbosity.Detailed)
                        {
                            m_logger?.LogText("Memory read:", false);
                            m_logger?.LogByteArray(l_registerBackup, false);
                            m_logger?.LogText(String.Format("<= [0x{0,8:X8}] (Saved registers)", 0x80), true);
                        }

                        for (int i_registerIndex = 0; i_registerIndex < 32; ++i_registerIndex)
                        {
                            UInt32 l_registerValue = 0x0;
                            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
                            {
                                if (l_registerBackup[i_registerIndex * 4 + i_byteIndex] == null)
                                {
                                    throw new RVEmulationException("Vector table memory range is not properly defined.");
                                }
                                l_registerValue <<= 8;
                                l_registerValue |= (UInt32)l_registerBackup[i_registerIndex * 4 + i_byteIndex];
                            }
                            WriteRegister((byte)i_registerIndex, l_registerValue);
                        }

                        // load return PC
                        byte?[] l_returnPC = m_memoryMap.Read(0x100, 4);

                        if (m_loggingVerbosity > Verbosity.Minimal)
                        {
                            m_logger?.LogText("Memory read:", false);
                            m_logger?.LogByteArray(l_returnPC, false);
                            m_logger?.LogText(String.Format("<= [0x{0,8:X8}] (Return PC)", 0x100), true);
                        }

                        m_programCounter = 0x0;
                        for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
                        {
                            if (l_returnPC[i_byteIndex] == null)
                            {
                                throw new RVEmulationException("Vector table memory range is not properly defined.");
                            }
                            m_programCounter <<= 8;
                            m_programCounter |= (UInt32)l_returnPC[i_byteIndex];
                        }

                        m_trapHandled = null;
                    }
                    else
                    {
                        m_logger?.LogText("Taking trap: InvalidInstruction (The iret instruction is not valid in the current context)", true);

                        // instruction is not defined outside of interrupt handlers
                        LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                    }

                    return false;
                }

                default:
                {
                    LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                    return false;
                }
            }
        }

        private void DecodeAndExecute(UInt32 instruction)
        {
            bool l_branch = false;
            UInt32 l_branchTo = 0x0;

            byte l_opcode = (byte)(instruction & 0x7F);

            switch (l_opcode)
            {
                case 0b0110111: // LUI
                {
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    UInt32 l_immediate = instruction & ~(UInt32)0xFFF;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} (imm)", l_immediate), true);
                    }

                    WriteRegister(l_destinationRegister, l_immediate);
                }
                break;

                case 0b0010111: // AUIPC
                {
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    UInt32 l_immediate = instruction & ~(UInt32)0xFFF;
                    UInt32 l_result = l_immediate + m_programCounter;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} = 0x{2,8:X8} (PC + imm)", m_programCounter, l_immediate, l_result), true);
                    }

                    WriteRegister(l_destinationRegister, l_result);
                }
                break;

                case 0b1101111: // JAL
                {
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    UInt32 l_offset = (instruction & 0x80000000) >> (31 - 20);
                    l_offset |= (instruction & 0x7FE00000) >> (21 - 1);
                    l_offset |= (instruction & 0x00100000) >> (20 - 11);
                    l_offset |= instruction & 0x000FF000;
                    l_offset = (UInt32)(((Int32)l_offset << (31 - 20)) >> (31 - 20)); // sign extend

                    l_branch = true;
                    l_branchTo = m_programCounter + l_offset;

                    UInt32 l_returnAddress = m_programCounter + 4;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 4 = 0x{1,8:X8} (PC + 4)", m_programCounter, l_returnAddress), true);
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} = 0x{2,8:X8} (PC + imm)", m_programCounter, l_offset, l_branchTo), true);
                    }

                    WriteRegister(l_destinationRegister, l_returnAddress);
                }
                break;

                case 0b1100111: // JALR
                {
                    if (((instruction >> 12) & 0x7) != 0b000) // check func3
                    {
                        LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                        return;
                    }

                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    UInt32 l_offset = (UInt32)(instruction >> 20);
                    l_offset = (UInt32)(((Int32)l_offset << (31 - 11)) >> (31 - 11)); // sign extend

                    UInt32 l_base = ReadRegister(l_sourceRegister1);

                    l_branch = true;
                    l_branchTo = (l_base + l_offset) & ~(UInt32)0x1;

                    UInt32 l_returnAddress = m_programCounter + 4;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 4 = 0x{1,8:X8} (PC + 4)", m_programCounter, l_returnAddress), true);
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} &~ 1 = 0x{2,8:X8} (x{3} + imm &~ 1)", l_base, l_offset, l_branchTo, l_sourceRegister1), true);
                    }

                    WriteRegister(l_destinationRegister, l_returnAddress);
                }
                break;

                case 0b1100011: // B-type
                {
                    byte l_func3 = (byte)((instruction >> 12) & 0x7);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    byte l_sourceRegister2 = (byte)((instruction >> 20) & 0x1F);
                    UInt32 l_offset = (instruction & 0x80000000) >> (31 - 12);
                    l_offset |= (instruction & 0x7E000000) >> (25 - 5);
                    l_offset |= (instruction & 0x00000F00) >> (8 - 1);
                    l_offset |= (instruction & 0x00000080) << (11 - 7);
                    l_offset = (UInt32)(((Int32)l_offset << (31 - 12)) >> (31 - 12)); // sign extend

                    bool? l_conditionsMet = MeetsBranchConditions(l_func3, l_sourceRegister1, l_sourceRegister2);
                    if (l_conditionsMet == null)
                    {
                        LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                        return;
                    }

                    if (l_conditionsMet == true)
                    {
                        l_branch = true;
                        l_branchTo = m_programCounter + l_offset;

                        m_logger?.LogText("Branch taken", true);

                        if (m_loggingVerbosity == Verbosity.Detailed)
                        {
                            m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} = 0x{2,8:X8} (PC + imm)", m_programCounter, l_offset, l_branchTo), true);
                        }
                    }
                    else
                    {
                        m_logger?.LogText("Branch not taken", true);
                    }
                }
                break;

                case 0b0000011: // loads
                {
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    byte l_func3 = (byte)((instruction >> 12) & 0x7);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    UInt32 l_offset = instruction >> 20;
                    l_offset = (UInt32)(((Int32)l_offset << (31 - 11)) >> (31 - 11));

                    UInt32 l_base = ReadRegister(l_sourceRegister1);
                    UInt32 l_loadAddress = l_base + l_offset;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} = 0x{2,8:X8} (x{3} + imm)", l_base, l_offset, l_loadAddress, l_sourceRegister1), true);
                    }

                    if (LoadFromMemory(l_func3, l_destinationRegister, l_loadAddress) == false)
                    {
                        return; // fault triggering is handled in LoadFromMemory
                    }
                }
                break;

                case 0b0100011: // stores
                {
                    byte l_func3 = (byte)((instruction >> 12) & 0x7);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    byte l_sourceRegister2 = (byte)((instruction >> 20) & 0x1F);
                    UInt32 l_offset = (instruction & 0xFE000000) >> (25 - 5);
                    l_offset |= (instruction & 0x00000F80) >> 7;
                    l_offset = (UInt32)(((Int32)l_offset << (31 - 11)) >> (31 - 11));

                    UInt32 l_base = ReadRegister(l_sourceRegister1);
                    UInt32 l_storeAddress = l_base + l_offset;

                    if (m_loggingVerbosity == Verbosity.Detailed)
                    {
                        m_logger?.LogText(String.Format("Executing: 0x{0,8:X8} + 0x{1,8:X8} = 0x{2,8:X8} (x{3} + imm)", l_base, l_offset, l_storeAddress, l_sourceRegister1), true);
                    }

                    UInt32 l_data = ReadRegister(l_sourceRegister2);

                    if (StoreToMemory(l_func3, l_storeAddress, l_data) == false)
                    {
                        return; // fault triggering is handled in StoreToMemory
                    }
                }
                break;

                case 0b0010011: // I-type
                {
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    byte l_func3 = (byte)((instruction >> 12) & 0x7);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    UInt32 l_immediate = instruction >> 20;

                    UInt16 l_operation = (UInt16)l_func3;
                    if (l_func3 == 0b001 || l_func3 == 0b101)
                    {
                        l_immediate &= 0x1F;

                        UInt16 l_func7 = (UInt16)((instruction >> 25) & 0x7F);
                        l_operation |= (UInt16)(l_func7 << 3);
                    }
                    else
                    {
                        l_immediate = (UInt32)(((Int32)l_immediate << (31 - 11)) >> (31 - 11));
                    }

                    if (ExecuteALU(l_operation, l_sourceRegister1, l_immediate, l_destinationRegister, true) == false)
                    {
                        return; // fault triggering is handled in ExecuteALU
                    }
                }
                break;

                case 0b0110011: // R-type
                {
                    byte l_func3 = (byte)((instruction >> 12) & 0x7);
                    byte l_func7 = (byte)((instruction >> 25) & 0x7F);
                    UInt16 l_extendedFunction = (UInt16)(((UInt16)l_func7 << 3) | l_func3);

                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    byte l_sourceRegister2 = (byte)((instruction >> 20) & 0x1F);

                    if (ExecuteALU(l_extendedFunction, l_sourceRegister1, l_sourceRegister2, l_destinationRegister, false) == false)
                    {
                        return; // fault triggering is handled in ExecuteALU
                    }
                }
                break;

                case 0b1110011: // system
                {
                    byte l_func3 = (byte)((instruction >> 12) & 0x7);
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);

                    if (l_func3 != 0 || l_destinationRegister != 0 || l_sourceRegister1 != 0)
                    {
                        LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                        return;
                    }

                    UInt16 l_func12 = (UInt16)(instruction >> 20);
                    if (ExecuteSystemInstruction(l_func12) == false)
                    {
                        return; // PC is handled in ExecuteSystemInstruction
                    }
                }
                break;

                default:
                {
                    LoadVector(RVVector.InvalidInstruction, CreateByteStream(m_programCounter)); // invalid instruction fault
                    return;
                }
            }

            m_programCounter = l_branch ? l_branchTo : m_programCounter + 4;
        }
    }
}
