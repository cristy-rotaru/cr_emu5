using System;
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
        bool m_handlingTrap = false;
        bool m_interruptsEnabled = false;
        bool m_waitForInterrupt = false;
        bool[] m_pendingInterrupts;

        List<UInt32> m_breakpoints;
        bool m_ebreakExecuted;

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

        public RVEmulator()
        {
            m_halted = true;
            m_haltReason = "";

            m_memoryMap = new RVMemoryMap();
            m_registerFile = new UInt32[32];
            m_pendingInterrupts = new bool[32];

            m_breakpoints = new List<UInt32>();
            m_ebreakExecuted = false;
        }

        public void Assemble(String code, RVLabelReferenceMap labelMap, Dictionary<UInt32, String> pseudoInstructions)
        {
            RVAssembler.Assemble(code, m_memoryMap, labelMap, pseudoInstructions);
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

        public void ResetProcessor()
        {
            for (int i_register = 0; i_register < 32; ++i_register)
            {
                m_registerFile[i_register] = 0x0;
            }

            m_handlingTrap = false;
            m_interruptsEnabled = false;
            m_programCounter = 0x0;
            byte?[] l_initialProgramCounter = m_memoryMap.Read(0x0, 4);
            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
            {
                if (l_initialProgramCounter[i_byteIndex] == null)
                {
                    throw new RVEmulationException("Vector table memory range is not properly defined.");
                }
                m_programCounter <<= 8;
                m_programCounter |= (UInt32)l_initialProgramCounter[i_byteIndex];
            }
        }

        public void SingleStep()
        {
            if (m_halted)
            {
                return;
            }

            m_ebreakExecuted = false;

            if (m_handlingTrap == false)
            {
                lock (m_pendingInterrupts)
                {
                    if (m_pendingInterrupts[1])
                    {
                        m_pendingInterrupts[1] = false;
                        LoadVector(RVVector.NMI, CreateByteStream(m_programCounter)); // NMI can not be masked
                        m_waitForInterrupt = false;
                        return;
                    }

                    if (m_interruptsEnabled)
                    {
                        for (int i_vectorIndex = 8; i_vectorIndex < 32; ++i_vectorIndex)
                        {
                            if (m_pendingInterrupts[i_vectorIndex])
                            {
                                m_pendingInterrupts[i_vectorIndex] = false;
                                LoadVector((RVVector)i_vectorIndex, CreateByteStream(m_programCounter));
                                m_waitForInterrupt = false;
                                return;
                            }
                        }
                    }
                }
            }

            if (m_waitForInterrupt)
            {
                return;
            }

            if ((m_programCounter & 0x3) != 0)
            {
                LoadVector(RVVector.MisalignedPC, CreateByteStream(m_programCounter)); // misaligned PC fault
                return;
            }

            byte?[] l_rawData = m_memoryMap.Read(m_programCounter, 4);
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

            DecodeAndExecute(l_instructionData);
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
            if (m_handlingTrap)
            {
                m_halted = true;
                m_haltReason = "Fault during exception/interrupt handling";
                return;
            }

            m_memoryMap.Write(0x100, contextInfo); // save trap info

            m_memoryMap.Write(0x80, CreateByteStream(m_registerFile)); // backup registers

            // load corresponding PC
            byte?[] l_vector = m_memoryMap.Read(0x4 * (UInt32)vectorIndex, 4);
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

            m_handlingTrap = true;
        }

        private void WriteRegister(byte index, UInt32 data)
        {
            if (index > 31)
            {
                throw new RVEmulationException("Register index out of range."); // if this happens I fucked something up really bad
            }

            if (index != 0)
            {
                m_registerFile[index] = data;
            }
        }

        private UInt32 ReadRegister(byte index)
        {
            if (index > 31)
            {
                throw new RVEmulationException("Register index out of range."); // if this happens I fucked something up really bad
            }

            return m_registerFile[index];
        }

        private bool StoreToMemory(byte opType, UInt32 address, UInt32 data)
        {
            switch (opType)
            {
                case 0b000: // SB
                {
                    try
                    {
                        byte[] l_writeData = new byte[1];
                        l_writeData[0] = (byte)(data & 0xFF);
                        try
                        {
                            m_memoryMap.Write(address, l_writeData);
                        }
                        catch (RVMemoryException)
                        {
                            LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                            return false;
                        }

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                case 0b001: // SH
                {
                    if ((address & 0x1) != 0)
                    {
                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    try
                    {
                        byte[] l_writeData = new byte[2];
                        l_writeData[0] = (byte)(data & 0xFF);
                        l_writeData[1] = (byte)((data >> 8) & 0xFF);
                        try
                        {
                            m_memoryMap.Write(address, l_writeData);
                        }
                        catch (RVMemoryException)
                        {
                            LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                            return false;
                        }

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                case 0b010: // SW
                {
                    if ((address & 0x3) != 0)
                    {
                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    try
                    {
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
                            LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                            return false;
                        }

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
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
                    if (l_rawData[0] == null)
                    {
                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0];
                    l_readValue = (UInt32)(((Int32)l_readValue << 24) >> 24); // sign extend
                }
                break;

                case 0b001: // LH
                {
                    if ((address & 0x1) != 0)
                    {
                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 2);
                    if (l_rawData[0] == null || l_rawData[1] == null)
                    {
                        LoadVector(RVVector.UndefinedMemory, CreateByteStream(m_programCounter, address)); // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0] | ((UInt32)l_rawData[1] << 8);
                    l_readValue = (UInt32)(((Int32)l_readValue << 16) >> 16); // sign extend
                }
                break;

                case 0b010: // LW
                {
                    if ((address & 0x3) != 0)
                    {
                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 4);
                    l_readValue = 0x0;
                    for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
                    {
                        if (l_rawData[i_byteIndex] == null)
                        {
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
                    if (l_rawData[0] == null)
                    {
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
                        LoadVector(RVVector.MisalignedMemory, CreateByteStream(m_programCounter, address)); // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 2);
                    if (l_rawData[0] == null || l_rawData[1] == null)
                    {
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

        private bool ExecuteALU(UInt16 opType, UInt32 operand1, UInt32 operand2, byte destination)
        {
            switch (opType)
            {
                case 0b0000000000: // ADD
                {
                    WriteRegister(destination, operand1 + operand2);
                }
                break;

                case 0b0100000000: // SUB
                {
                    WriteRegister(destination, operand1 - operand2);
                }
                break;

                case 0b0000000001: // SLL
                {
                    WriteRegister(destination, operand1 << (int)operand2);
                }
                break;

                case 0b0000000010: // SLT
                {
                    WriteRegister(destination, (Int32)operand1 < (Int32)operand2 ? (UInt32)1 : (UInt32)0);
                }
                break;

                case 0b0000000011: // SLTU
                {
                    WriteRegister(destination, operand1 < operand2 ? (UInt32)1 : (UInt32)0);
                }
                break;

                case 0b0000000100: // XOR
                {
                    WriteRegister(destination, operand1 ^ operand2);
                }
                break;

                case 0b0000000101: // SRL
                {
                    WriteRegister(destination, operand1 >> (int)operand2);
                }
                break;

                case 0b0100000101: // SRA
                {
                    WriteRegister(destination, (UInt32)((Int32)operand1 >> (int)operand2));
                }
                break;

                case 0b0000000110: // OR
                {
                    WriteRegister(destination, operand1 | operand2);
                }
                break;

                case 0b0000000111: // AND
                {
                    WriteRegister(destination, operand1 & operand2);
                }
                break;

                case 0b0000001000: // MUL
                {
                    WriteRegister(destination, operand1 * operand2);
                }
                break;

                case 0b0000001001: // MULH
                {
                    Int64 l_extendedOperand1 = (Int64)operand1;
                    Int64 l_extendedOperand2 = (Int64)operand2;

                    // sign extend
                    l_extendedOperand1 <<= 32;
                    l_extendedOperand2 <<= 32;
                    l_extendedOperand1 >>= 32;
                    l_extendedOperand2 >>= 32;

                    Int64 l_result = l_extendedOperand1 * l_extendedOperand2;
                    l_result >>= 32;

                    WriteRegister(destination, (UInt32)l_result);
                }
                break;

                case 0b0000001010: // MULHSU
                {
                    Int64 l_extendedOperand1 = (Int64)operand1;
                    Int64 l_extendedOperand2 = (Int64)operand2;

                    // sign extend
                    l_extendedOperand1 <<= 32;
                    l_extendedOperand1 >>= 32;

                    l_extendedOperand2 &= 0xFFFFFFFF;

                    Int64 l_result = l_extendedOperand1 * l_extendedOperand2;
                    l_result >>= 32;

                    WriteRegister(destination, (UInt32)l_result);
                }
                break;

                case 0b0000001011: // MULHU
                {
                    UInt64 l_result = (UInt64)operand1 * (UInt64)operand2;
                    l_result >>= 32;

                    WriteRegister(destination, (UInt32)l_result);
                }
                break;

                case 0b0000001100: // DIV
                {
                    if (operand2 == 0 || (operand1 == 0x80000000 && operand2 == 0xFFFFFFFF))
                    {
                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, (UInt32)((Int32)operand1 / (Int32)operand2));
                }
                break;

                case 0b0000001101: // DIVU
                {
                    if (operand2 == 0)
                    {
                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, operand1 / operand2);
                }
                break;

                case 0b0000001110: // REM
                {
                    if (operand2 == 0)
                    {
                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, (UInt32)((Int32)operand1 % (Int32)operand2));
                }
                break;

                case 0b0000001111: // REMU
                {
                    if (operand2 == 0)
                    {
                        LoadVector(RVVector.DivisionBy0, CreateByteStream(m_programCounter)); // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, operand1 % operand2);
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
                    return l_data1 == l_data2;
                }

                case 0b001: // BNE
                {
                    return l_data1 != l_data2;
                }

                case 0b100: // BLT
                {
                    return (Int32)l_data1 < (Int32)l_data2;
                }

                case 0b101: // BGE
                {
                    return (Int32)l_data1 >= (Int32)l_data2;
                }

                case 0b110: // BLTU
                {
                    return l_data1 < l_data2;
                }

                case 0b111: // BGEU
                {
                    return l_data1 >= l_data2;
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
                    LoadVector(RVVector.ECALL, CreateByteStream(m_programCounter + 4));
                    return false;
                }

                case 0x001: // ebreak
                {
                    m_ebreakExecuted = true;
                    return true;
                }

                case 0xFFF: // hlt
                {
                    m_halted = true;
                    m_haltReason = "Halt instruction executed";
                    return false;
                }

                case 0xFFE: // rst
                {
                    ResetProcessor();
                    return false;
                }

                case 0x107: // ien
                {
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
                    if (m_handlingTrap == false) // no effect if already in interrupt handler
                    {
                        m_waitForInterrupt = true;
                    }

                    return true;
                }

                case 0x102: // iret
                {
                    if (m_handlingTrap)
                    {
                        // restore registers
                        byte?[] l_registerBackup = m_memoryMap.Read(0x80, 128);
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

                        m_handlingTrap = false;
                    }
                    else
                    {
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

                    WriteRegister(l_destinationRegister, l_immediate);
                }
                break;

                case 0b0010111: // AUIPC
                {
                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    UInt32 l_immediate = instruction & ~(UInt32)0xFFF;

                    WriteRegister(l_destinationRegister, l_immediate + m_programCounter);
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

                    WriteRegister(l_destinationRegister, m_programCounter + 4);
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

                    WriteRegister(l_destinationRegister, m_programCounter + 4);
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
                    if (LoadFromMemory(l_func3, l_destinationRegister, l_base + l_offset) == false)
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
                    UInt32 l_data = ReadRegister(l_sourceRegister2);

                    if (StoreToMemory(l_func3, l_base + l_offset, l_data) == false)
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

                    if (ExecuteALU(l_operation, ReadRegister(l_sourceRegister1), l_immediate, l_destinationRegister) == false)
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

                    if (ExecuteALU(l_extendedFunction, ReadRegister(l_sourceRegister1), ReadRegister(l_sourceRegister2), l_destinationRegister) == false)
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
