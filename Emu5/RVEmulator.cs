using System;

namespace Emu5
{
    public class RVEmulationException : Exception
    {
        public RVEmulationException(String message) : base(message) { }
    }

    public class RVEmulator
    {
        RVMemoryMap m_memoryMap;
        UInt32[] m_registerFile;

        UInt32 m_programCounter;

        public RVEmulator()
        {
            m_memoryMap = new RVMemoryMap();
            m_registerFile = new UInt32[32];
        }

        public void Assemble(String code)
        {
            RVAssembler.Assemble(code, m_memoryMap);
        }

        public void ResetProcessor()
        {
            for (int i_register = 0; i_register < 32; ++i_register)
            {
                m_registerFile[i_register] = 0x0;
            }

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
            if ((m_programCounter & 0x3) != 0)
            {
                // misaligned PC fault
                return;
            }

            byte?[] l_rawData = m_memoryMap.Read(m_programCounter, 4);
            UInt32 l_instructionData = 0x0;
            for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
            {
                if (l_rawData[i_byteIndex] == null)
                {
                    // undefined memory address fault
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
                        m_memoryMap.Write(address, l_writeData);

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
                        // misaligned access fault
                        return false;
                    }

                    try
                    {
                        byte[] l_writeData = new byte[2];
                        l_writeData[0] = (byte)(data & 0xFF);
                        l_writeData[1] = (byte)((data >> 8) & 0xFF);
                        m_memoryMap.Write(address, l_writeData);

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
                        // misaligned access fault
                        return false;
                    }

                    try
                    {
                        byte[] l_writeData = new byte[4];
                        l_writeData[0] = (byte)(data & 0xFF);
                        l_writeData[1] = (byte)((data >> 8) & 0xFF);
                        l_writeData[2] = (byte)((data >> 16) & 0xFF);
                        l_writeData[3] = (byte)((data >> 24) & 0xFF);
                        m_memoryMap.Write(address, l_writeData);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                default:
                {
                    // invalid instruction fault
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
                        // undefined memory address fault
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
                        // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 2);
                    if (l_rawData[0] == null || l_rawData[1] == null)
                    {
                        // undefined memory address fault
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
                        // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 4);
                    l_readValue = 0x0;
                    for (int i_byteIndex = 3; i_byteIndex >= 0; --i_byteIndex)
                    {
                        if (l_rawData[i_byteIndex] == null)
                        {
                            // undefined memory address fault
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
                        // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0];
                }
                break;

                case 0b101: // LHU
                {
                    if ((address & 0x1) != 0)
                    {
                        // misaligned access fault
                        return false;
                    }

                    byte?[] l_rawData = m_memoryMap.Read(address, 2);
                    if (l_rawData[0] == null || l_rawData[1] == null)
                    {
                        // undefined memory address fault
                        return false;
                    }

                    l_readValue = (UInt32)l_rawData[0] | ((UInt32)l_rawData[1] << 8);
                }
                break;

                default:
                {
                    // invalid instruction fault
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
                        // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, (UInt32)((Int32)operand1 / (Int32)operand2));
                }
                break;

                case 0b0000001101: // DIVU
                {
                    if (operand2 == 0)
                    {
                        // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, operand1 / operand2);
                }
                break;

                case 0b0000001110: // REM
                {
                    if (operand2 == 0)
                    {
                        // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, (UInt32)((Int32)operand1 % (Int32)operand2));
                }
                break;

                case 0b0000001111: // REMU
                {
                    if (operand2 == 0)
                    {
                        // division by 0 or division overflow fault
                        return false;
                    }

                    WriteRegister(destination, operand1 % operand2);
                }
                break;

                default:
                {
                    // invalid instruction fault
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
                        // invalid instruction fault
                        return;
                    }

                    byte l_destinationRegister = (byte)((instruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((instruction >> 15) & 0x1F);
                    UInt16 l_offset = (UInt16)(instruction >> 20);
                    l_offset = (UInt16)(((Int16)l_offset << (31 - 11)) >> (31 - 11)); // sign extend

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
                        // invalid instruction fault
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
                    // NOT YET IMPLEMENTED
                }
                break;

                default:
                {
                    // invalid instruction fault
                    return;
                }
            }

            m_programCounter = l_branch ? l_branchTo : m_programCounter + 4;
        }
    }
}
