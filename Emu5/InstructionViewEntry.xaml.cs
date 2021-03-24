using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for InstructionViewEntry.xaml
    /// </summary>
    public partial class InstructionViewEntry : UserControl
    {
        static Dictionary<byte, String> s_bTypeInstructions = null; // key = func3
        static Dictionary<byte, String> s_loadTypeInstructions = null; // key = func3
        static Dictionary<byte, String> s_sTypeInstructions = null; // key = func3
        static Dictionary<byte, String> s_iTypeInstructions = null; // key = func3
        static Dictionary<UInt16, String> s_shiftTypeInstructions = null; // key = {func7, func3}
        static Dictionary<UInt16, String> s_rTypeInstructions = null; // key = {func7, func3}
        static Dictionary<UInt16, String> s_systemInstructions = null; // key = func12

        UInt32 m_address;

        public InstructionViewEntry()
        {
            InitializeComponent();

            if (s_bTypeInstructions == null)
            {
                s_bTypeInstructions = new Dictionary<byte, String>();

                s_bTypeInstructions.Add(0b000, "beq");
                s_bTypeInstructions.Add(0b001, "bne");
                s_bTypeInstructions.Add(0b100, "blt");
                s_bTypeInstructions.Add(0b101, "bge");
                s_bTypeInstructions.Add(0b110, "bltu");
                s_bTypeInstructions.Add(0b111, "bgeu");
            }

            if (s_loadTypeInstructions == null)
            {
                s_loadTypeInstructions = new Dictionary<byte, String>();

                s_loadTypeInstructions.Add(0b000, "lb");
                s_loadTypeInstructions.Add(0b001, "lh");
                s_loadTypeInstructions.Add(0b010, "lw");
                s_loadTypeInstructions.Add(0b100, "lbu");
                s_loadTypeInstructions.Add(0b101, "lhu");
            }

            if (s_sTypeInstructions == null)
            {
                s_sTypeInstructions = new Dictionary<byte, String>();

                s_sTypeInstructions.Add(0b000, "sb");
                s_sTypeInstructions.Add(0b001, "sh");
                s_sTypeInstructions.Add(0b010, "sw");
            }

            if (s_iTypeInstructions == null)
            {
                s_iTypeInstructions = new Dictionary<byte, String>();

                s_iTypeInstructions.Add(0b000, "addi");
                s_iTypeInstructions.Add(0b010, "slti");
                s_iTypeInstructions.Add(0b011, "sltiu");
                s_iTypeInstructions.Add(0b100, "xori");
                s_iTypeInstructions.Add(0b110, "ori");
                s_iTypeInstructions.Add(0b111, "andi");
            }

            if (s_shiftTypeInstructions == null)
            {
                s_shiftTypeInstructions = new Dictionary<UInt16, String>();

                s_shiftTypeInstructions.Add(0b0000000001, "slli");
                s_shiftTypeInstructions.Add(0b0000000101, "srli");
                s_shiftTypeInstructions.Add(0b0100000101, "srai");
            }

            if (s_rTypeInstructions == null)
            {
                s_rTypeInstructions = new Dictionary<UInt16, String>();

                s_rTypeInstructions.Add(0b0000000000, "add");
                s_rTypeInstructions.Add(0b0100000000, "sub");
                s_rTypeInstructions.Add(0b0000000001, "sll");
                s_rTypeInstructions.Add(0b0000000010, "slt");
                s_rTypeInstructions.Add(0b0000000011, "sltu");
                s_rTypeInstructions.Add(0b0000000100, "xor");
                s_rTypeInstructions.Add(0b0000000101, "srl");
                s_rTypeInstructions.Add(0b0100000101, "sra");
                s_rTypeInstructions.Add(0b0000000110, "or");
                s_rTypeInstructions.Add(0b0000000111, "and");
                s_rTypeInstructions.Add(0b0000001000, "mul");
                s_rTypeInstructions.Add(0b0000001001, "mulh");
                s_rTypeInstructions.Add(0b0000001010, "mulhsu");
                s_rTypeInstructions.Add(0b0000001011, "mulhu");
                s_rTypeInstructions.Add(0b0000001100, "div");
                s_rTypeInstructions.Add(0b0000001101, "divu");
                s_rTypeInstructions.Add(0b0000001110, "rem");
                s_rTypeInstructions.Add(0b0000001111, "remu");
            }

            if (s_systemInstructions == null)
            {
                s_systemInstructions = new Dictionary<UInt16, String>();

                s_systemInstructions.Add(0x000, "ecall");
                s_systemInstructions.Add(0x001, "ebreak");
                s_systemInstructions.Add(0xFFF, "hlt");
                s_systemInstructions.Add(0xFFE, "rst");
                s_systemInstructions.Add(0x107, "ien");
                s_systemInstructions.Add(0x106, "idis");
                s_systemInstructions.Add(0x105, "wfi");
            }
        }

        public void DisplayData(bool breakpoint, UInt32 address, byte?[] rawData, String annotation)
        {
            if (rawData.Length != 4)
            {
                throw new Exception("Invalid data length.");
            }

            m_address = address;

            UInt32 l_instructionData = 0x0;
            bool l_validInstructionData = true;

            for (int i_byteIndex = 0; i_byteIndex < 4; ++i_byteIndex)
            {
                if (rawData[i_byteIndex] == null)
                {
                    l_validInstructionData = false;
                    break;
                }
                byte l_byte = (byte)rawData[i_byteIndex];
                l_instructionData |= ((UInt32)l_byte) << (8 * i_byteIndex);
            }

            textBlockAddress.Text = String.Format("{0,8:X8}", address);
            textBlockAnnotation.Text = annotation;

            if (l_validInstructionData)
            {
                Tuple<String, String> l_decodedInstruction = DecodeIntruction(l_instructionData);

                textBlockInstruction.Text = l_decodedInstruction.Item1;
                textBlockRawValue.Text = l_decodedInstruction.Item2;
            }
            else
            {
                textBlockInstruction.Text = "";
                textBlockRawValue.Text = "";
            }    

            buttonToggleBreakpoint.Content = breakpoint ? new Ellipse { Height = 11, Width = 11 } : null;
        }

        private Tuple<String, String> DecodeIntruction(UInt32 encodedInstruction)
        {// returns a pair of strings (1st is the decoded instruction) (2nd is the split instruction bits)
            Tuple<String, String> l_notAnInstruction = new Tuple<String, String>("", String.Format("0x{0,8:X8}", encodedInstruction));

            byte l_opcode = (byte)(encodedInstruction & 0x7F);
            switch (l_opcode)
            {
                case 0b0110111: // LUI
                {
                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    UInt32 l_immediate = encodedInstruction >> 12;

                    String l_assembly = String.Format("lui x{0}, 0x{1:X}", l_destinationRegister, l_immediate);
                    String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(20, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b0010111: // AUIPC
                {
                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    UInt32 l_immediate = encodedInstruction >> 12;

                    String l_assembly = String.Format("auipc x{0}, 0x{1:X}", l_destinationRegister, l_immediate);
                    String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(20, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b1101111: // JAL
                {
                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    UInt32 l_immediate = (encodedInstruction & 0x80000000) >> (31 - 20);
                    l_immediate |= (encodedInstruction & 0x7FE00000) >> (21 - 1);
                    l_immediate |= (encodedInstruction & 0x00100000) >> (20 - 11);
                    l_immediate |= encodedInstruction & 0x000FF000;
                    Int32 l_offset = ((Int32)l_immediate << (31 - 20)) >> (31 - 20); // sign extend
                    l_immediate = encodedInstruction >> 12;

                    String l_assembly = String.Format("jal x{0}, {1}", l_destinationRegister, l_offset);
                    String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(20, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b1100111: // JALR
                {
                    if (((encodedInstruction >> 12) & 0x7) != 0b000) // check func3
                    {
                        return l_notAnInstruction;
                    }

                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                    UInt16 l_immediate = (UInt16)(encodedInstruction >> 20);
                    Int32 l_offset = ((Int32)l_immediate << (31 - 11)) >> (31 - 11); // sign extend

                    String l_assembly = String.Format("jalr x{0}, {1}(x{2})", l_destinationRegister, l_offset, l_sourceRegister1);
                    String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(12, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + "_000_" + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b1100011: // B-type
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    String l_mnemonic;

                    if (s_bTypeInstructions.TryGetValue(l_func3, out l_mnemonic) == false)
                    {
                        return l_notAnInstruction;
                    }

                    byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                    byte l_sourceRegister2 = (byte)((encodedInstruction >> 20) & 0x1F);
                    UInt32 l_immediate = (encodedInstruction & 0x80000000) >> (31 - 12);
                    l_immediate |= (encodedInstruction & 0x7E000000) >> (25 - 5);
                    l_immediate |= (encodedInstruction & 0x00000F00) >> (8 - 1);
                    l_immediate |= (encodedInstruction & 0x00000080) << (11 - 7);
                    Int32 l_offset = ((Int32)l_immediate << (31 - 12)) >> (31 - 12);
                    byte l_immHigh = (byte)(encodedInstruction >> 25);
                    byte l_immLow = (byte)((encodedInstruction >> 7) & 0x1F);

                    String l_assembly = String.Format("{0} x{1}, x{2}, {3}", l_mnemonic, l_sourceRegister1, l_sourceRegister2, l_offset);
                    String l_splitBits = Convert.ToString(l_immHigh, 2).PadLeft(7, '0') + '_' + Convert.ToString(l_sourceRegister2, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_immLow, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b0000011: // loads
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    String l_mnemonic;

                    if (s_loadTypeInstructions.TryGetValue(l_func3, out l_mnemonic) == false)
                    {
                        return l_notAnInstruction;
                    }

                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                    UInt32 l_immediate = encodedInstruction >> 20;
                    Int32 l_offset = ((Int32)l_immediate << (31 - 11)) >> (31 - 11);

                    String l_assembly = String.Format("{0} x{1}, {2}(x{3})", l_mnemonic, l_destinationRegister, l_offset, l_sourceRegister1);
                    String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(12, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b0100011: // stores
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    String l_mnemonic;

                    if (s_sTypeInstructions.TryGetValue(l_func3, out l_mnemonic) == false)
                    {
                        return l_notAnInstruction;
                    }

                    byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                    byte l_sourceRegister2 = (byte)((encodedInstruction >> 20) & 0x1F);
                    UInt32 l_immediate = (encodedInstruction & 0xFE000000) >> (25 - 5);
                    l_immediate |= (encodedInstruction & 0x00000F80) >> 7;
                    Int32 l_offset = ((Int32)l_immediate << (31 - 11)) >> (31 - 11);
                    byte l_immHigh = (byte)(l_immediate >> 5);
                    byte l_immLow = (byte)(l_immediate & 0x1F);

                    String l_assembly = String.Format("{0} x{1}, {2}(x{3})", l_mnemonic, l_sourceRegister2, l_offset, l_sourceRegister1);
                    String l_splitBits = Convert.ToString(l_immHigh, 2).PadLeft(7, '0') + '_' + Convert.ToString(l_sourceRegister2, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_immLow, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b0010011: // I-type
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    byte l_func7 = (byte)((encodedInstruction >> 25) & 0x7F);
                    UInt16 l_extendedFunction = (UInt16)(((UInt16)l_func7 << 3) | l_func3);
                    String l_mnemonic;

                    if (s_iTypeInstructions.TryGetValue(l_func3, out l_mnemonic))
                    {
                        byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                        byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                        UInt32 l_immediate = encodedInstruction >> 20;
                        Int32 l_offset = ((Int32)l_immediate << (31 - 11)) >> (31 - 11);

                        String l_assembly = String.Format("{0} x{1}, x{2}, {3}", l_mnemonic, l_destinationRegister, l_sourceRegister1, l_offset);
                        String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(12, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                        return new Tuple<String, String>(l_assembly, l_splitBits);
                    }
                    else if (s_shiftTypeInstructions.TryGetValue(l_extendedFunction, out l_mnemonic))
                    {
                        byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                        byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                        byte l_shift = (byte)((encodedInstruction >> 20) & 0x1F);

                        String l_assembly = String.Format("{0} x{1}, x{2}, {3}", l_mnemonic, l_destinationRegister, l_sourceRegister1, l_shift);
                        String l_splitBits = Convert.ToString(l_func7, 2).PadLeft(7, '0') + '_' + Convert.ToString(l_shift, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                        return new Tuple<String, String>(l_assembly, l_splitBits);
                    }
                    else
                    {
                        return l_notAnInstruction;
                    }
                }

                case 0b0110011: // R-type
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    byte l_func7 = (byte)((encodedInstruction >> 25) & 0x7F);
                    UInt16 l_extendedFunction = (UInt16)(((UInt16)l_func7 << 3) | l_func3);
                    String l_mnemonic;

                    if (s_rTypeInstructions.TryGetValue(l_extendedFunction, out l_mnemonic) == false)
                    {
                        return l_notAnInstruction;
                    }

                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                    byte l_sourceRegister2 = (byte)((encodedInstruction >> 20) & 0x1F);

                    String l_assembly = String.Format("{0} x{1}, x{2}, x{3}", l_mnemonic, l_destinationRegister, l_sourceRegister1, l_sourceRegister2);
                    String l_splitBits = Convert.ToString(l_func7, 2).PadLeft(7, '0') + '_' + Convert.ToString(l_sourceRegister2, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b1110011: // system
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                    byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);

                    if (l_func3 != 0 || l_destinationRegister != 0 || l_sourceRegister1 != 0)
                    {
                        return l_notAnInstruction;
                    }

                    UInt16 l_func12 = (UInt16)(encodedInstruction >> 20);
                    String l_mnemonic;

                    if (s_systemInstructions.TryGetValue(l_func12, out l_mnemonic) == false)
                    {
                        return l_notAnInstruction;
                    }

                    String l_splitBits = Convert.ToString((UInt32)l_func12, 2).PadLeft(12, '0') + "_00000_000_00000_" + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                    return new Tuple<String, String>(l_mnemonic, l_splitBits);
                }

                default:
                {
                    return l_notAnInstruction;
                }
            }
        }

        public bool Highlighted
        {
            set
            {
                this.Background = value ? Brushes.Yellow : Brushes.Transparent;
            }
        }

        public UInt32 GetAddress()
        {
            return m_address;
        }
    }
}
