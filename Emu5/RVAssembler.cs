﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Emu5
{
    public enum RVDataType
    {
        DB, DH, DW, STR, STRZ
    }

    public class RVAssemblyException : Exception
    {
        private readonly uint m_line, m_column;

        public RVAssemblyException(String message, uint line, uint column) : base(message)
        {
            m_line = line;
            m_column = column;
        }

        public uint Line
        {
            get
            {
                return m_line;
            }
        }

        public uint Column
        {
            get
            {
                return m_column;
            }
        }
    }

    public struct Interval
    {
        public UInt32 start;
        public UInt32 end;
    }

    public class RVAssembler
    {
        private static object s_lock = new object();
        private static RVAssembler s_instance;

        Dictionary<String, RVDataType> m_dataTypeDictionary;

        private struct RVInstructionBuilder
        {
            public UInt32 startAddress;
            public byte[] data;
            public bool pendingLabel;
            public RVInstructionDescription description;
            public String label;
            public uint labelLine;
            public uint labelColumn;
        }

        private struct RVDataBuilder
        {
            public UInt32 startAddress;
            public byte[] data;
            public bool pendingLabel;
            public String label;
            public uint labelLine;
            public uint labelColumn;
        }

        private RVAssembler()
        {
            m_dataTypeDictionary = new Dictionary<string, RVDataType>();

            m_dataTypeDictionary.Add("db", RVDataType.DB);
            m_dataTypeDictionary.Add("DB", RVDataType.DB);

            m_dataTypeDictionary.Add("dh", RVDataType.DH);
            m_dataTypeDictionary.Add("DH", RVDataType.DH);

            m_dataTypeDictionary.Add("dw", RVDataType.DW);
            m_dataTypeDictionary.Add("DW", RVDataType.DW);

            m_dataTypeDictionary.Add("str", RVDataType.STR);
            m_dataTypeDictionary.Add("STR", RVDataType.STR);

            m_dataTypeDictionary.Add("strz", RVDataType.STRZ);
            m_dataTypeDictionary.Add("STRZ", RVDataType.STRZ);
        }

        static public void Assemble(String code, RVMemoryMap memoryMap, RVLabelReferenceMap labelMap, Dictionary<UInt32, String> pseudoInstructions, bool useIntegratedEcallHandler, UInt32 ecallBase = 0xFFFFF000)
        {
            RVToken[][] l_tokens = RVParser.Tokenize(code);
            
            UInt32 l_currentAddress = 0x00000000;

            Dictionary<String, UInt32> l_labelDictionary = new Dictionary<String, UInt32>();
            List<RVInstructionBuilder> l_instructionList = new List<RVInstructionBuilder>();
            List<RVDataBuilder> l_dataList = new List<RVDataBuilder>();
            List<Interval> l_memoryIntervals = new List<Interval>();
            Stream l_ecallHandlerStream = null;

            if (useIntegratedEcallHandler)
            {
                l_ecallHandlerStream = new MemoryStream(Properties.Resources.ecall_handler);
                l_memoryIntervals.Add(new Interval { start = ecallBase, end = (UInt32)(ecallBase + l_ecallHandlerStream.Length - 1) });
            }

            pseudoInstructions.Clear();

            for (int i_line = 0; i_line < l_tokens.Length; ++i_line)
            {
                RVToken[] l_tokenLine = l_tokens[i_line];

                if (l_tokenLine.Length == 0)
                {
                    continue; // skip if line is empty
                }

                switch (l_tokenLine[0].type)
                {
                    case RVTokenType.Label:
                    {
                        String l_labelName = (String)l_tokenLine[0].value;

                        if (l_tokenLine.Length == 1 || l_tokenLine[1].type != RVTokenType.Separator || (char)l_tokenLine[1].value != ':')
                        {
                            throw new RVAssemblyException("Expected ':' after label definition.", l_tokenLine[0].line, l_tokenLine[0].column + (uint)l_labelName.Length);
                        }

                        if (l_labelDictionary.ContainsKey(l_labelName))
                        {
                            throw new RVAssemblyException("Redefinition of label \"" + l_labelName + "\".", l_tokenLine[0].line, l_tokenLine[0].column);
                        }
                        l_labelDictionary.Add(l_labelName, l_currentAddress);

                        if (l_tokenLine.Length > 2)
                        {
                            throw new RVAssemblyException("Expected new line after label definition.", l_tokenLine[1].line, 0);
                        }
                    }
                    break;

                    case RVTokenType.Address:
                    {
                        l_currentAddress = (UInt32)l_tokenLine[0].value;

                        if (l_tokenLine.Length > 1)
                        {
                            throw new RVAssemblyException("Expected new line after address identifier.", l_tokenLine[0].line, 0);
                        }
                    }
                    break;

                    case RVTokenType.DataType:
                    {
                        if (l_tokenLine.Length == 1)
                        {
                            throw new RVAssemblyException("Empty data definition.", l_tokenLine[0].line, 0);
                        }

                        RVDataType l_dataType = (RVDataType)l_tokenLine[0].value;
                        UInt32 l_dataSize = l_dataType == RVDataType.DB ? 1u : (l_dataType == RVDataType.DH ? 2u : (l_dataType == RVDataType.DW ? 4u : 0u));

                        for (int i_data = 1; i_data < l_tokenLine.Length; ++i_data)
                        {
                            if ((i_data & 1) == 0) // expect separator ',' on even positions
                            {
                                if (l_tokenLine[i_data].type != RVTokenType.Separator || (Char)l_tokenLine[i_data].value != ',')
                                {
                                    throw new RVAssemblyException("Expected ',' between values.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                }
                            }
                            else
                            {
                                switch (l_tokenLine[i_data].type)
                                {
                                    case RVTokenType.Label:
                                    {
                                        if (l_dataType != RVDataType.DW)
                                        {
                                            throw new RVAssemblyException("Incompatible data type.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                        }

                                        // allign address to 32 bits
                                        l_currentAddress += 0x3u;
                                        l_currentAddress &= ~0x3u;

                                        if (CheckInterval(l_memoryIntervals, new Interval { start = l_currentAddress, end = l_currentAddress + 3 }) == false)
                                        {
                                            throw new RVAssemblyException("Memory location defined more than once", l_tokenLine[i_data].line, 0);
                                        }

                                        RVDataBuilder l_dataBuilder = new RVDataBuilder { startAddress = l_currentAddress, pendingLabel = true, label = (String)l_tokenLine[i_data].value, labelLine = l_tokenLine[i_data].line, labelColumn = l_tokenLine[i_data].column };
                                        l_dataList.Add(l_dataBuilder);

                                        l_currentAddress += 4;
                                    }
                                    break;

                                    case RVTokenType.Integer:
                                    {
                                        if (l_dataType == RVDataType.STR || l_dataType == RVDataType.STRZ)
                                        {
                                            throw new RVAssemblyException("Incompatible data type.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                        }

                                        // check encodable range
                                        UInt32 l_number = (UInt32)l_tokenLine[i_data].value;
                                        if (l_dataSize < 4)
                                        {
                                            UInt32 l_checkMask = (UInt32)(-1 << (8 * (int)l_dataSize - 1));
                                            if ((l_number & l_checkMask) != l_checkMask)
                                            {
                                                l_checkMask <<= 1;
                                                if ((l_number & l_checkMask) != 0)
                                                {
                                                    throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                                }
                                            }
                                        }

                                        // allign to data size
                                        UInt32 l_allignMask = l_dataSize - 1;
                                        l_currentAddress += l_allignMask;
                                        l_currentAddress &= ~l_allignMask;

                                        if (CheckInterval(l_memoryIntervals, new Interval { start = l_currentAddress, end = l_currentAddress + l_dataSize - 1 }) == false)
                                        {
                                            throw new RVAssemblyException("Memory location defined more than once.", l_tokenLine[i_data].line, 0);
                                        }

                                        RVDataBuilder l_dataBuilder = new RVDataBuilder { startAddress = l_currentAddress, data = new byte[l_dataSize], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < l_dataSize; ++i_byteIndex)
                                        {
                                            l_dataBuilder.data[i_byteIndex] = (byte)l_number;
                                            l_number >>= 8;
                                        }
                                        l_dataList.Add(l_dataBuilder);

                                        l_currentAddress += l_dataSize;
                                    }
                                    break;

                                    case RVTokenType.Char:
                                    {
                                        if (l_dataType == RVDataType.STR || l_dataType == RVDataType.STRZ)
                                        {
                                            throw new RVAssemblyException("Incompatible data type.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                        }

                                        Char l_character = (Char)l_tokenLine[i_data].value;

                                        // allign to data size
                                        UInt32 l_allignMask = l_dataSize - 1;
                                        l_currentAddress += l_allignMask;
                                        l_currentAddress &= ~l_allignMask;

                                        if (CheckInterval(l_memoryIntervals, new Interval { start = l_currentAddress, end = l_currentAddress + l_dataSize - 1 }) == false)
                                        {
                                            throw new RVAssemblyException("Memory location defined more than once.", l_tokenLine[i_data].line, 0);
                                        }

                                        RVDataBuilder l_dataBuilder = new RVDataBuilder { startAddress = l_currentAddress, data = new byte[l_dataSize], pendingLabel = false };
                                        l_dataBuilder.data[0] = (byte)l_character;
                                        for (int i_byteIndex = 1; i_byteIndex < l_dataSize; ++i_byteIndex)
                                        {
                                            l_dataBuilder.data[i_byteIndex] = 0;
                                        }
                                        l_dataList.Add(l_dataBuilder);

                                        l_currentAddress += l_dataSize;
                                    }
                                    break;

                                    case RVTokenType.String:
                                    {
                                        if (l_dataType != RVDataType.STR && l_dataType != RVDataType.STRZ)
                                        {
                                            throw new RVAssemblyException("Incompatible data type.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                        }

                                        String l_string = (String)l_tokenLine[i_data].value;
                                        UInt32 l_length = (UInt32)l_string.Length;
                                        if (l_dataType == RVDataType.STRZ)
                                        {
                                            ++l_length;
                                        }

                                        if (CheckInterval(l_memoryIntervals, new Interval { start = l_currentAddress, end = l_currentAddress + l_length - 1 }) == false)
                                        {
                                            throw new RVAssemblyException("Memory location defined more than once.", l_tokenLine[i_data].line, 0);
                                        }

                                        RVDataBuilder l_dataBuilder = new RVDataBuilder { startAddress = l_currentAddress, data = new byte[l_length], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < l_string.Length; ++i_byteIndex)
                                        {
                                            l_dataBuilder.data[i_byteIndex] = (byte)l_string[i_byteIndex];
                                        }
                                        if (l_dataType == RVDataType.STRZ)
                                        {
                                            l_dataBuilder.data[l_length - 1] = 0;
                                        }
                                        l_dataList.Add(l_dataBuilder);

                                        l_currentAddress += l_length;
                                    }
                                    break;

                                    default:
                                    {
                                        throw new RVAssemblyException("Unexpected token at this location.", l_tokenLine[i_data].line, l_tokenLine[i_data].column);
                                    }
                                }
                            }
                        }
                    }
                    break;

                    case RVTokenType.Instruction:
                    {
                        RVInstructionDescription l_instructionDescription = (RVInstructionDescription)l_tokenLine[0].value;

                        // allign address to 32 bits
                        l_currentAddress += 0x3u;
                        l_currentAddress &= ~0x3u;

                        if (CheckInterval(l_memoryIntervals, new Interval { start = l_currentAddress, end = l_currentAddress + l_instructionDescription.size - 1 }) == false)
                        {
                            throw new RVAssemblyException("Memory location defined more than once.", l_tokenLine[0].line, 0);
                        }

                        switch (l_instructionDescription.type)
                        {
                            case RVInstructionType.R:
                            {
                                if (l_tokenLine.Length != 6)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                }

                                if (l_tokenLine[5].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                l_instructionDescription.rs2 = (RVRegister)l_tokenLine[5].value;

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func7) & 0x7F) << 25;

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.I:
                            {
                                if (l_tokenLine.Length != 6)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                }

                                if (l_tokenLine[5].type != RVTokenType.Integer && l_tokenLine[5].type != RVTokenType.Char)
                                {
                                    throw new RVAssemblyException("Expected integer immediate.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                UInt32 l_immediate = (l_tokenLine[5].type == RVTokenType.Integer) ? (UInt32)l_tokenLine[5].value : (UInt32)(char)l_tokenLine[5].value;
                                UInt32 l_mask = 0xFFFFF800;
                                if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                l_instructionDescription.imm = l_immediate;

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0xFFF) << 20;

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.Shift:
                            {
                                if (l_tokenLine.Length != 6)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                }

                                if (l_tokenLine[5].type != RVTokenType.Integer)
                                {
                                    throw new RVAssemblyException("Expected integer immediate.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                UInt32 l_immediate = (UInt32)l_tokenLine[5].value;
                                if ((l_immediate & ~0x1F) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                l_instructionDescription.shamt = (byte)l_immediate;

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.shamt) & 0x1F) << 20;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func7) & 0x7F) << 25;

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.S:
                            {
                                if (l_tokenLine.Length != 7)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Integer)
                                {
                                    throw new RVAssemblyException("Expected integer immediate.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                UInt32 l_mask = 0xFFFFF800;
                                if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.imm = l_immediate;

                                if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != '(')
                                {
                                    throw new RVAssemblyException("Expected '('.", l_tokenLine[4].line, l_tokenLine[4].column);
                                }

                                if (l_tokenLine[5].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                l_instructionDescription.rs1 = (RVRegister)l_tokenLine[5].value;

                                if (l_tokenLine[6].type != RVTokenType.Separator || (char)l_tokenLine[6].value != ')')
                                {
                                    throw new RVAssemblyException("Expected ')'.", l_tokenLine[6].line, l_tokenLine[6].column);
                                }

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1F) << 7;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0xFE0) << (25 - 5);

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.IOffset:
                            {
                                if (l_tokenLine.Length != 7)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Integer)
                                {
                                    throw new RVAssemblyException("Expected integer immediate.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                UInt32 l_mask = 0xFFFFF800;
                                if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.imm = l_immediate;

                                if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != '(')
                                {
                                    throw new RVAssemblyException("Expected '('.", l_tokenLine[4].line, l_tokenLine[4].column);
                                }

                                if (l_tokenLine[5].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                                l_instructionDescription.rs1 = (RVRegister)l_tokenLine[5].value;

                                if (l_tokenLine[6].type != RVTokenType.Separator || (char)l_tokenLine[6].value != ')')
                                {
                                    throw new RVAssemblyException("Expected ')'.", l_tokenLine[6].line, l_tokenLine[6].column);
                                }

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0xFFF) << 20;

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.B:
                            {
                                if (l_tokenLine.Length != 6)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rs1 = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.rs2 = (RVRegister)l_tokenLine[3].value;

                                if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                }

                                if (l_tokenLine[5].type == RVTokenType.Integer)
                                {
                                    UInt32 l_immediate = (UInt32)l_tokenLine[5].value;
                                    UInt32 l_mask = 0xFFFFF000;
                                    if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                    {
                                        throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                    }
                                    if ((l_immediate & 0x1) != 0)
                                    {
                                        throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[5].line, l_tokenLine[5].column);
                                    }
                                    l_instructionDescription.imm = l_immediate;

                                    UInt32 l_encodedInstruction = 0x0;
                                    l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                    RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                    for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                    {
                                        l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                        l_encodedInstruction >>= 8;
                                    }
                                    l_instructionList.Add(l_instructionBuilder);
                                }
                                else if (l_tokenLine[5].type == RVTokenType.Label)
                                {
                                    RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[5].value, labelLine = l_tokenLine[5].line, labelColumn = l_tokenLine[5].column };
                                    l_instructionList.Add(l_instructionBuilder);
                                }
                                else
                                {
                                    throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                }
                            }
                            break;

                            case RVInstructionType.U:
                            {
                                if (l_tokenLine.Length != 4)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type != RVTokenType.Integer)
                                {
                                    throw new RVAssemblyException("Expected integer immediate.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                UInt32 l_mask = 0xFFF80000;
                                if ((l_immediate & l_mask) != l_mask && (l_immediate & (l_mask << 1)) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                                l_instructionDescription.imm = l_immediate;

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0xFFFFF) << 12;

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.J:
                            {
                                if (l_tokenLine.Length != 4)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                if (l_tokenLine[1].type != RVTokenType.Register)
                                {
                                    throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                }
                                l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                {
                                    throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                }

                                if (l_tokenLine[3].type == RVTokenType.Integer)
                                {
                                    UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                    UInt32 l_mask = 0xFFF00000;
                                    if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                    {
                                        throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                    }
                                    if ((l_immediate & 0x1) != 0)
                                    {
                                        throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                    }
                                    l_instructionDescription.imm = l_immediate;

                                    UInt32 l_encodedInstruction = 0x0;
                                    l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0xFF000);
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) << (20 - 11);
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7FE) << (21 - 1);
                                    l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x100000) << (31 - 20);

                                    RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                    for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                    {
                                        l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                        l_encodedInstruction >>= 8;
                                    }
                                    l_instructionList.Add(l_instructionBuilder);
                                }
                                else if (l_tokenLine[3].type == RVTokenType.Label)
                                {
                                    RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                    l_instructionList.Add(l_instructionBuilder);
                                }
                                else
                                {
                                    throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                }
                            }
                            break;

                            case RVInstructionType.System:
                            {
                                if (l_tokenLine.Length != 1)
                                {
                                    throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                }

                                UInt32 l_encodedInstruction = 0x0;
                                l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x1F) << 12;
                                l_encodedInstruction |= (((UInt32)l_instructionDescription.func12) & 0xFFF) << 20;

                                RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                {
                                    l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                    l_encodedInstruction >>= 8;
                                }
                                l_instructionList.Add(l_instructionBuilder);
                            }
                            break;

                            case RVInstructionType.Pseudo:
                            {
                                switch (l_instructionDescription.mnemonic)
                                {
                                    case "la":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Label)
                                        {
                                            throw new RVAssemblyException("Expected label identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " " + (String)l_tokenLine[3].value);
                                        pseudoInstructions.Add(l_currentAddress + 4, l_instructionDescription.mnemonic + " " + (String)l_tokenLine[3].value);
                                    }
                                    break;

                                    case "li":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Integer)
                                        {
                                            throw new RVAssemblyException("Expected integer immediate.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        UInt32 l_immediate = (UInt32)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction1, l_actualInstruction2;

                                        l_actualInstruction1 = 0x37; // LUI instruction
                                        l_actualInstruction1 |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        if ((l_immediate & 0x800) == 0) // checking bit 12 of the immediate
                                        {
                                            l_actualInstruction1 |= l_immediate & 0xFFFFF000;
                                            l_actualInstruction2 = 0x13; // ADDI
                                        }
                                        else
                                        {
                                            l_actualInstruction1 |= ~l_immediate & 0xFFFFF000;
                                            l_actualInstruction2 = 0x4013; // XORI
                                        }
                                        l_actualInstruction2 |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction2 |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 15;
                                        l_actualInstruction2 |= (l_immediate & 0xFFF) << 20;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[8], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction1 & 0xFF);
                                            l_instructionBuilder.data[i_byteIndex + 4] = (byte)(l_actualInstruction2 & 0xFF);
                                            l_actualInstruction1 >>= 8;
                                            l_actualInstruction2 >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " 0x" + String.Format("{0:X}", l_immediate));
                                        pseudoInstructions.Add(l_currentAddress + 4, l_instructionDescription.mnemonic + " 0x" + String.Format("{0:X}", l_immediate));
                                    }
                                    break;

                                    case "nop":
                                    {
                                        if (l_tokenLine.Length != 1)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        UInt32 l_actualInstruction = 0x7033; // AND x0, x0, x0

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic);
                                    }
                                    break;

                                    case "mv":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0x33; // ADD
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs1);
                                    }
                                    break;

                                    case "not":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0xFFF04013; // XORI .., .., -1
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs1);
                                    }
                                    break;

                                    case "neg":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0x40000033; // SUB .., .., ..
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs2);
                                    }
                                    break;

                                    case "seqz":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0x00103013; // SLTIU .., .., 1
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs1);
                                    }
                                    break;

                                    case "snez":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0x3033; // SLTU .., x0, ..
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs2);
                                    }
                                    break;

                                    case "sltz":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0x2033; // SLT .., .., x0
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs1);
                                    }
                                    break;

                                    case "sgtz":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rd = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[3].value;

                                        UInt32 l_actualInstruction = 0x2033; // SLT .., x0, ..
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rd + ", x" + (int)l_instructionDescription.rs2);
                                    }
                                    break;

                                    case "beqz":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b000;
                                        l_instructionDescription.rs2 = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[3].type == RVTokenType.Label)
                                        {
                                            l_instructionDescription.type = RVInstructionType.B;
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[3].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bnez":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b001;
                                        l_instructionDescription.rs2 = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[3].type == RVTokenType.Label)
                                        {
                                            l_instructionDescription.type = RVInstructionType.B;
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[3].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "blez":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b101;
                                        l_instructionDescription.rs1 = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[3].type == RVTokenType.Label)
                                        {
                                            l_instructionDescription.type = RVInstructionType.B;
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", " + (String)l_tokenLine[3].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bgez":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b101;
                                        l_instructionDescription.rs2 = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[3].type == RVTokenType.Label)
                                        {
                                            l_instructionDescription.type = RVInstructionType.B;
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[3].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bltz":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b100;
                                        l_instructionDescription.rs2 = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[3].type == RVTokenType.Label)
                                        {
                                            l_instructionDescription.type = RVInstructionType.B;
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[3].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bgtz":
                                    {
                                        if (l_tokenLine.Length != 4)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b100;
                                        l_instructionDescription.rs1 = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[3].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[3].line, l_tokenLine[3].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[3].type == RVTokenType.Label)
                                        {
                                            l_instructionDescription.type = RVInstructionType.B;
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[3].value, labelLine = l_tokenLine[3].line, labelColumn = l_tokenLine[3].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", " + (String)l_tokenLine[3].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bgt":
                                    {
                                        if (l_tokenLine.Length != 6)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b100;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                        }

                                        if (l_tokenLine[5].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[5].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[5].type == RVTokenType.Label)
                                        {
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[5].value, labelLine = l_tokenLine[5].line, labelColumn = l_tokenLine[5].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[5].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "ble":
                                    {
                                        if (l_tokenLine.Length != 6)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b101;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                        }

                                        if (l_tokenLine[5].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[5].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[5].type == RVTokenType.Label)
                                        {
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[5].value, labelLine = l_tokenLine[5].line, labelColumn = l_tokenLine[5].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[5].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bgtu":
                                    {
                                        if (l_tokenLine.Length != 6)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b110;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                        }

                                        if (l_tokenLine[5].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[5].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[5].type == RVTokenType.Label)
                                        {
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[5].value, labelLine = l_tokenLine[5].line, labelColumn = l_tokenLine[5].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[5].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "bleu":
                                    {
                                        if (l_tokenLine.Length != 6)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.B;
                                        l_instructionDescription.opcode = 0b1100011;
                                        l_instructionDescription.func3 = 0b111;

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs2 = (RVRegister)l_tokenLine[1].value;

                                        if (l_tokenLine[2].type != RVTokenType.Separator || (char)l_tokenLine[2].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[2].line, l_tokenLine[2].column);
                                        }

                                        if (l_tokenLine[3].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[3].line, l_tokenLine[3].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[3].value;

                                        if (l_tokenLine[4].type != RVTokenType.Separator || (char)l_tokenLine[4].value != ',')
                                        {
                                            throw new RVAssemblyException("Expected separator ','.", l_tokenLine[4].line, l_tokenLine[4].column);
                                        }

                                        if (l_tokenLine[5].type == RVTokenType.Integer)
                                        {
                                            UInt32 l_immediate = (UInt32)l_tokenLine[5].value;
                                            UInt32 l_mask = 0xFFFFF000;
                                            if ((l_immediate & l_mask) != l_mask && (l_immediate & l_mask) != 0)
                                            {
                                                throw new RVAssemblyException("Number exceeds encodable range.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            if ((l_immediate & 0x1) != 0)
                                            {
                                                throw new RVAssemblyException("Branch offset must not be an odd number.", l_tokenLine[5].line, l_tokenLine[5].column);
                                            }
                                            l_instructionDescription.imm = l_immediate;

                                            UInt32 l_encodedInstruction = 0x0;
                                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);

                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                            {
                                                l_instructionBuilder.data[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                                                l_encodedInstruction >>= 8;
                                            }
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (int)l_immediate);
                                        }
                                        else if (l_tokenLine[5].type == RVTokenType.Label)
                                        {
                                            RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[5].value, labelLine = l_tokenLine[5].line, labelColumn = l_tokenLine[5].column };
                                            l_instructionList.Add(l_instructionBuilder);

                                            pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs2 + ", x" + (int)l_instructionDescription.rs1 + ", " + (String)l_tokenLine[5].value);
                                        }
                                        else
                                        {
                                            throw new RVAssemblyException("Expected integer immediate or label identifier.", l_tokenLine[5].line, l_tokenLine[5].column);
                                        }
                                    }
                                    break;

                                    case "j":
                                    {
                                        if (l_tokenLine.Length != 2)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }
                                        l_instructionDescription.type = RVInstructionType.J;
                                        l_instructionDescription.opcode = 0b1101111;
                                        l_instructionDescription.rd = RVRegister.x0;

                                        if (l_tokenLine[1].type != RVTokenType.Label)
                                        {
                                            throw new RVAssemblyException("Expected label identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[1].value, labelLine = l_tokenLine[1].line, labelColumn = l_tokenLine[1].column };
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " " + (String)l_tokenLine[1].value);
                                    }
                                    break;

                                    case "jr":
                                    {
                                        if (l_tokenLine.Length != 2)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Register)
                                        {
                                            throw new RVAssemblyException("Expected register identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }
                                        l_instructionDescription.rs1 = (RVRegister)l_tokenLine[1].value;

                                        UInt32 l_actualInstruction = 0x67;
                                        l_actualInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " x" + (int)l_instructionDescription.rs1);
                                    }
                                    break;

                                    case "ret":
                                    {
                                        if (l_tokenLine.Length != 1)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        UInt32 l_actualInstruction = 0x8067;

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, data = new byte[4], pendingLabel = false };
                                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                                        {
                                            l_instructionBuilder.data[i_byteIndex] = (byte)(l_actualInstruction & 0xFF);
                                            l_actualInstruction >>= 8;
                                        }
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic);
                                    }
                                    break;

                                    case "call":
                                    {
                                        if (l_tokenLine.Length != 2)
                                        {
                                            throw new RVAssemblyException("Incorrect instruction parameters.", l_tokenLine[0].line, l_tokenLine[0].column);
                                        }

                                        if (l_tokenLine[1].type != RVTokenType.Label)
                                        {
                                            throw new RVAssemblyException("Expected label identifier.", l_tokenLine[1].line, l_tokenLine[1].column);
                                        }

                                        RVInstructionBuilder l_instructionBuilder = new RVInstructionBuilder { startAddress = l_currentAddress, pendingLabel = true, description = l_instructionDescription, label = (String)l_tokenLine[1].value, labelLine = l_tokenLine[1].line, labelColumn = l_tokenLine[1].column };
                                        l_instructionList.Add(l_instructionBuilder);

                                        pseudoInstructions.Add(l_currentAddress, l_instructionDescription.mnemonic + " " + (String)l_tokenLine[1].value);
                                        pseudoInstructions.Add(l_currentAddress + 4, l_instructionDescription.mnemonic + " " + (String)l_tokenLine[1].value);
                                    }
                                    break;

                                    default:
                                    {
                                        throw new Exception("If you see this, I definetely fucked something up.");
                                    }
                                }
                            }
                            break;
                        }

                        l_currentAddress += l_instructionDescription.size;
                    }
                    break;

                    default:
                    {
                        throw new RVAssemblyException("Unexpected token at this location.", l_tokenLine[0].line, l_tokenLine[0].column);
                    }
                }
            }

            // handle label dependent entries
            for (int i_index = 0; i_index < l_dataList.Count; ++i_index)
            {
                RVDataBuilder l_dataBuilder = l_dataList[i_index];
                if (l_dataBuilder.pendingLabel == true)
                {
                    if (l_labelDictionary.ContainsKey(l_dataBuilder.label) == false)
                    {
                        throw new RVAssemblyException("Undefined label \"" + l_dataBuilder.label + "\"", l_dataBuilder.labelLine, l_dataBuilder.labelColumn);
                    }

                    UInt32 l_labelValue = l_labelDictionary[l_dataBuilder.label];

                    l_dataBuilder.data = new byte[4];
                    for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                    {
                        l_dataBuilder.data[i_byteIndex] = (byte)(l_labelValue & 0xFF);
                        l_labelValue >>= 8;
                    }
                    l_dataBuilder.pendingLabel = false;

                    l_dataList[i_index] = l_dataBuilder;
                }
            }

            for (int i_index = 0; i_index < l_instructionList.Count; ++i_index)
            {
                RVInstructionBuilder l_instructionBuilder = l_instructionList[i_index];
                if (l_instructionBuilder.pendingLabel == true)
                {
                    if (l_labelDictionary.ContainsKey(l_instructionBuilder.label) == false)
                    {
                        throw new RVAssemblyException("Undefined label \"" + l_instructionBuilder.label + "\"", l_instructionBuilder.labelLine, l_instructionBuilder.labelColumn);
                    }

                    RVInstructionDescription l_instructionDescription = l_instructionBuilder.description;
                    byte[] l_instructionBytes;

                    if (l_instructionDescription.type == RVInstructionType.Pseudo)
                    {
                        l_instructionBytes = new byte[8];

                        if (l_instructionDescription.mnemonic == "la")
                        {
                            UInt32 l_addressToLoad = l_labelDictionary[l_instructionBuilder.label];

                            UInt32 l_actualInstruction1, l_actualInstruction2;

                            l_actualInstruction1 = 0x37; // LUI instruction
                            l_actualInstruction1 |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                            if ((l_addressToLoad & 0x800) == 0) // checking bit 12 of the immediate
                            {
                                l_actualInstruction1 |= l_addressToLoad & 0xFFFFF000;
                                l_actualInstruction2 = 0x6013; // ORI
                            }
                            else
                            {
                                l_actualInstruction1 |= ~l_addressToLoad & 0xFFFFF000;
                                l_actualInstruction2 = 0x4013; // XORI
                            }
                            l_actualInstruction2 |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                            l_actualInstruction2 |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 15;
                            l_actualInstruction2 |= (l_addressToLoad & 0xFFF) << 20;

                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                            {
                                l_instructionBytes[i_byteIndex] = (byte)(l_actualInstruction1 & 0xFF);
                                l_instructionBytes[i_byteIndex + 4] = (byte)(l_actualInstruction2 & 0xFF);
                                l_actualInstruction1 >>= 8;
                                l_actualInstruction2 >>= 8;
                            }
                        }
                        else if (l_instructionDescription.mnemonic == "call")
                        {
                            UInt32 l_offset = l_labelDictionary[l_instructionBuilder.label] - l_instructionBuilder.startAddress;

                            UInt32 l_actualInstruction1, l_actualInstruction2;

                            l_actualInstruction1 = 0x97; // AUIPC x1, ..
                            l_actualInstruction1 |= (l_offset & 0xFFFFF000) + ((l_offset & 0x800) << 1);
                            l_actualInstruction2 = 0x80E7; // JALR x1, ..(x1)
                            l_actualInstruction2 |= l_offset << 20;

                            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                            {
                                l_instructionBytes[i_byteIndex] = (byte)(l_actualInstruction1 & 0xFF);
                                l_instructionBytes[i_byteIndex + 4] = (byte)(l_actualInstruction2 & 0xFF);
                                l_actualInstruction1 >>= 8;
                                l_actualInstruction2 >>= 8;
                            }
                        }
                        else
                        {
                            throw new Exception("If you see this, I definetely fucked something up.");
                        }
                    }
                    else
                    {
                        UInt32 l_offset = l_labelDictionary[l_instructionBuilder.label] - l_instructionBuilder.startAddress;
                        UInt32 l_mask;

                        if (l_instructionDescription.type == RVInstructionType.B)
                        {
                            l_mask = 0xFFFFF000;
                        }
                        else if (l_instructionDescription.type == RVInstructionType.J)
                        {
                            l_mask = 0xFFF00000;
                        }
                        else
                        {
                            throw new Exception("If you see this, I definetely fucked something up.");
                        }

                        if ((l_offset & l_mask) != l_mask && (l_offset & l_mask) != 0)
                        {
                            throw new RVAssemblyException("Target is too far.", l_instructionBuilder.labelLine, l_instructionBuilder.labelColumn);
                        }
                        l_instructionDescription.imm = l_offset;

                        UInt32 l_encodedInstruction = 0x0;
                        if (l_instructionDescription.type == RVInstructionType.B)
                        {
                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) >> (11 - 7);
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1E) << (8 - 1);
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.func3) & 0x7) << 12;
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs1) & 0x1F) << 15;
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rs2) & 0x1F) << 20;
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7E0) << (25 - 5);
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x1000) << (31 - 12);
                        }
                        else if (l_instructionDescription.type == RVInstructionType.J)
                        {
                            l_encodedInstruction |= ((UInt32)l_instructionDescription.opcode) & 0x7F;
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.rd) & 0x1F) << 7;
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0xFF000);
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x800) << (20 - 11);
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x7FE) << (21 - 1);
                            l_encodedInstruction |= (((UInt32)l_instructionDescription.imm) & 0x100000) << (31 - 20);
                        }

                        l_instructionBytes = new byte[4];
                        for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
                        {
                            l_instructionBytes[i_byteIndex] = (byte)(l_encodedInstruction & 0xFF);
                            l_encodedInstruction >>= 8;
                        }
                    }

                    l_instructionBuilder.pendingLabel = false;
                    l_instructionBuilder.data = l_instructionBytes;

                    l_instructionList[i_index] = l_instructionBuilder;
                }
            }

            if (useIntegratedEcallHandler)
            {
                byte[] l_ecallHandlerData = new byte[l_ecallHandlerStream.Length];
                l_ecallHandlerStream.Read(l_ecallHandlerData, 0, (int)l_ecallHandlerStream.Length);
                memoryMap.Write(ecallBase, l_ecallHandlerData);
            }

            foreach (RVDataBuilder i_dataBuilder in l_dataList)
            {
                memoryMap.Write(i_dataBuilder.startAddress, i_dataBuilder.data);
            }

            foreach (RVInstructionBuilder i_instructionBuilder in l_instructionList)
            {
                memoryMap.Write(i_instructionBuilder.startAddress, i_instructionBuilder.data);
            }

            labelMap.Clear();
            foreach (String i_label in l_labelDictionary.Keys)
            {
                labelMap.Add(l_labelDictionary[i_label], i_label);
            }
        }

        static private bool CheckInterval(List<Interval> existingIntervals, Interval newInterval)
        {
            // check if interval is not overwritten
            foreach (Interval i_existing in existingIntervals)
            {
                if (newInterval.start >= i_existing.start && newInterval.start <= i_existing.end)
                {
                    return false;
                }
                if (newInterval.end >= i_existing.start && newInterval.end <= i_existing.end)
                {
                    return false;
                }
                if (i_existing.start >= newInterval.start && i_existing.start <= newInterval.end)
                {
                    return false;
                }
                if (i_existing.end >= newInterval.start && i_existing.end <= newInterval.end)
                {
                    return false;
                }
            }

            bool l_merged = false;
            int l_mergeIndex = -1;
            for (int i_index = 0; i_index < existingIntervals.Count; ++i_index)
            {
                Interval l_interval = existingIntervals[i_index];
                if (newInterval.start != 0x00000000 && newInterval.start == (l_interval.end + 1))
                { // merge at the end
                    l_interval.end = newInterval.end;
                    existingIntervals[i_index] = l_interval;

                    l_merged = true;
                    l_mergeIndex = i_index;

                    break;
                }
                if (newInterval.end != 0xFFFFFFFF && (newInterval.end + 1) == l_interval.start)
                { // merge at the beginning
                    l_interval.start = newInterval.start;
                    existingIntervals[i_index] = l_interval;

                    l_merged = true;
                    l_mergeIndex = i_index;

                    break;
                }
            }

            if (l_merged)
            {
                Interval l_intervalToMerge = existingIntervals[l_mergeIndex];

                for (int i_index = 0; i_index < existingIntervals.Count; ++i_index) // check if can merge more
                {
                    if (i_index != l_mergeIndex)
                    {
                        Interval l_interval = existingIntervals[i_index];
                        if (l_interval.start != 0x00000000 && l_interval.start == (l_intervalToMerge.end + 1))
                        { // merge at the end
                            l_intervalToMerge.end = l_interval.end;
                            existingIntervals[l_mergeIndex] = l_intervalToMerge;

                            existingIntervals.RemoveAt(i_index);
                            break;
                        }
                        if (l_interval.end != 0xFFFFFFFF && (l_interval.end + 1) == l_intervalToMerge.start)
                        {
                            l_intervalToMerge.start = l_interval.start;
                            existingIntervals[l_mergeIndex] = l_intervalToMerge;

                            existingIntervals.RemoveAt(i_index);
                            break;
                        }
                    }
                }
            }
            else
            {
                existingIntervals.Add(newInterval);
            }

            return true;
        }

        static public RVDataType? GetDataTypeByString(String data)
        {
            lock (s_lock)
            {
                if (s_instance == null)
                {
                    s_instance = new RVAssembler();
                }
            }

            RVDataType l_dataType;
            if (s_instance.m_dataTypeDictionary.TryGetValue(data, out l_dataType))
            {
                return l_dataType;
            }

            return null;
        }
    }
}
