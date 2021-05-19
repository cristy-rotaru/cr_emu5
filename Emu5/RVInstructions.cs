using System;
using System.Collections.Generic;

namespace Emu5
{
    public enum RVInstructionType
    {
        R,
        I,
        S,
        B,
        U,
        J,
        IOffset,
        Shift,
        System,
        Pseudo
    }

    public enum RVRegister
    {
        x0 = 0,
        x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15, x16, x17, x18, x19, x20, x21, x22, x23, x24, x25, x26, x27, x28, x29, x30, x31
    }

    public struct RVInstructionDescription
    {
        public RVInstructionType type;
        public String mnemonic;
        public byte size;
        public byte opcode;
        public byte func3;
        public byte func7;
        public short func12;
        public byte shamt;
        public UInt32 imm;
        public RVRegister rs1;
        public RVRegister rs2;
        public RVRegister rd;
    }

    public class RVInstructions
    {
        private static object s_lock = new object();
        private static RVInstructions s_instance = null;

        Dictionary<String, RVInstructionDescription> m_instructionDictionary;
        Dictionary<String, RVRegister> m_registerDictionary;

        Dictionary<byte, String> m_bTypeInstructions = null; // key = func3
        Dictionary<byte, String> m_loadTypeInstructions = null; // key = func3
        Dictionary<byte, String> m_sTypeInstructions = null; // key = func3
        Dictionary<byte, String> m_iTypeInstructions = null; // key = func3
        Dictionary<UInt16, String> m_shiftTypeInstructions = null; // key = {func7, func3}
        Dictionary<UInt16, String> m_rTypeInstructions = null; // key = {func7, func3}
        Dictionary<UInt16, String> m_systemInstructions = null; // key = func12

        private RVInstructions()
        {
            m_instructionDictionary = new Dictionary<string, RVInstructionDescription>();

            m_instructionDictionary.Add("lui", LUI);
            m_instructionDictionary.Add("LUI", LUI);

            m_instructionDictionary.Add("auipc", AUIPC);
            m_instructionDictionary.Add("AUIPC", AUIPC);

            m_instructionDictionary.Add("jal", JAL);
            m_instructionDictionary.Add("JAL", JAL);

            m_instructionDictionary.Add("jalr", JALR);
            m_instructionDictionary.Add("JALR", JALR);

            m_instructionDictionary.Add("beq", BEQ);
            m_instructionDictionary.Add("BEQ", BEQ);

            m_instructionDictionary.Add("bne", BNE);
            m_instructionDictionary.Add("BNE", BNE);

            m_instructionDictionary.Add("blt", BLT);
            m_instructionDictionary.Add("BLT", BLT);

            m_instructionDictionary.Add("bge", BGE);
            m_instructionDictionary.Add("BGE", BGE);

            m_instructionDictionary.Add("bltu", BLTU);
            m_instructionDictionary.Add("BLTU", BLTU);

            m_instructionDictionary.Add("bgeu", BGEU);
            m_instructionDictionary.Add("BGEU", BGEU);

            m_instructionDictionary.Add("lb", LB);
            m_instructionDictionary.Add("LB", LB);

            m_instructionDictionary.Add("lh", LH);
            m_instructionDictionary.Add("LH", LH);

            m_instructionDictionary.Add("lw", LW);
            m_instructionDictionary.Add("LW", LW);

            m_instructionDictionary.Add("lbu", LBU);
            m_instructionDictionary.Add("LBU", LBU);

            m_instructionDictionary.Add("lhu", LHU);
            m_instructionDictionary.Add("LHU", LHU);

            m_instructionDictionary.Add("sb", SB);
            m_instructionDictionary.Add("SB", SB);

            m_instructionDictionary.Add("sh", SH);
            m_instructionDictionary.Add("SH", SH);

            m_instructionDictionary.Add("sw", SW);
            m_instructionDictionary.Add("SW", SW);

            m_instructionDictionary.Add("addi", ADDI);
            m_instructionDictionary.Add("ADDI", ADDI);

            m_instructionDictionary.Add("slti", SLTI);
            m_instructionDictionary.Add("SLTI", SLTI);

            m_instructionDictionary.Add("sltiu", SLTIU);
            m_instructionDictionary.Add("SLTIU", SLTIU);

            m_instructionDictionary.Add("xori", XORI);
            m_instructionDictionary.Add("XORI", XORI);

            m_instructionDictionary.Add("ori", ORI);
            m_instructionDictionary.Add("ORI", ORI);

            m_instructionDictionary.Add("andi", ANDI);
            m_instructionDictionary.Add("ANDI", ANDI);

            m_instructionDictionary.Add("slli", SLLI);
            m_instructionDictionary.Add("SLLI", SLLI);

            m_instructionDictionary.Add("srli", SRLI);
            m_instructionDictionary.Add("SRLI", SRLI);

            m_instructionDictionary.Add("srai", SRAI);
            m_instructionDictionary.Add("SRAI", SRAI);

            m_instructionDictionary.Add("add", ADD);
            m_instructionDictionary.Add("ADD", ADD);

            m_instructionDictionary.Add("sub", SUB);
            m_instructionDictionary.Add("SUB", SUB);

            m_instructionDictionary.Add("sll", SLL);
            m_instructionDictionary.Add("SLL", SLL);

            m_instructionDictionary.Add("slt", SLT);
            m_instructionDictionary.Add("SLT", SLT);

            m_instructionDictionary.Add("sltu", SLTU);
            m_instructionDictionary.Add("SLTU", SLTU);

            m_instructionDictionary.Add("xor", XOR);
            m_instructionDictionary.Add("XOR", XOR);

            m_instructionDictionary.Add("srl", SRL);
            m_instructionDictionary.Add("SRL", SRL);

            m_instructionDictionary.Add("sra", SRA);
            m_instructionDictionary.Add("SRA", SRA);

            m_instructionDictionary.Add("or", OR);
            m_instructionDictionary.Add("OR", OR);

            m_instructionDictionary.Add("and", AND);
            m_instructionDictionary.Add("AND", AND);

            m_instructionDictionary.Add("ecall", ECALL);
            m_instructionDictionary.Add("ECALL", ECALL);

            m_instructionDictionary.Add("ebreak", EBREAK);
            m_instructionDictionary.Add("EBREAK", EBREAK);

            m_instructionDictionary.Add("mul", MUL);
            m_instructionDictionary.Add("MUL", MUL);

            m_instructionDictionary.Add("mulh", MULH);
            m_instructionDictionary.Add("MULH", MULH);

            m_instructionDictionary.Add("mulhsu", MULHSU);
            m_instructionDictionary.Add("MULHSU", MULHSU);

            m_instructionDictionary.Add("mulhu", MULHU);
            m_instructionDictionary.Add("MULHU", MULHU);

            m_instructionDictionary.Add("div", DIV);
            m_instructionDictionary.Add("DIV", DIV);

            m_instructionDictionary.Add("divu", DIVU);
            m_instructionDictionary.Add("DIVU", DIVU);

            m_instructionDictionary.Add("rem", REM);
            m_instructionDictionary.Add("REM", REM);

            m_instructionDictionary.Add("remu", REMU);
            m_instructionDictionary.Add("REMU", REMU);

            m_instructionDictionary.Add("hlt", HLT);
            m_instructionDictionary.Add("HLT", HLT);

            m_instructionDictionary.Add("rst", RST);
            m_instructionDictionary.Add("RST", RST);

            m_instructionDictionary.Add("ien", IEN);
            m_instructionDictionary.Add("IEN", IEN);

            m_instructionDictionary.Add("idis", IDIS);
            m_instructionDictionary.Add("IDIS", IDIS);

            m_instructionDictionary.Add("wfi", WFI);
            m_instructionDictionary.Add("WFI", WFI);

            m_instructionDictionary.Add("iret", IRET);
            m_instructionDictionary.Add("IRET", IRET);

            m_instructionDictionary.Add("la", LA);
            m_instructionDictionary.Add("LA", LA);

            m_instructionDictionary.Add("li", LI);
            m_instructionDictionary.Add("LI", LI);

            m_instructionDictionary.Add("nop", NOP);
            m_instructionDictionary.Add("NOP", NOP);

            m_instructionDictionary.Add("mv", MV);
            m_instructionDictionary.Add("MV", MV);

            m_instructionDictionary.Add("not", NOT);
            m_instructionDictionary.Add("NOT", NOT);

            m_instructionDictionary.Add("neg", NEG);
            m_instructionDictionary.Add("NEG", NEG);

            m_instructionDictionary.Add("seqz", SEQZ);
            m_instructionDictionary.Add("SEQZ", SEQZ);

            m_instructionDictionary.Add("snez", SNEZ);
            m_instructionDictionary.Add("SNEZ", SNEZ);

            m_instructionDictionary.Add("sltz", SLTZ);
            m_instructionDictionary.Add("SLTZ", SLTZ);

            m_instructionDictionary.Add("sgtz", SGTZ);
            m_instructionDictionary.Add("SGTZ", SGTZ);

            m_instructionDictionary.Add("beqz", BEQZ);
            m_instructionDictionary.Add("BEQZ", BEQZ);

            m_instructionDictionary.Add("bnez", BNEZ);
            m_instructionDictionary.Add("BNEZ", BNEZ);

            m_instructionDictionary.Add("blez", BLEZ);
            m_instructionDictionary.Add("BLEZ", BLEZ);

            m_instructionDictionary.Add("bgez", BGEZ);
            m_instructionDictionary.Add("BGEZ", BGEZ);

            m_instructionDictionary.Add("bltz", BLTZ);
            m_instructionDictionary.Add("BLTZ", BLTZ);

            m_instructionDictionary.Add("bgtz", BGTZ);
            m_instructionDictionary.Add("BGTZ", BGTZ);

            m_instructionDictionary.Add("bgt", BGT);
            m_instructionDictionary.Add("BGT", BGT);

            m_instructionDictionary.Add("ble", BLE);
            m_instructionDictionary.Add("BLE", BLE);

            m_instructionDictionary.Add("bgtu", BGTU);
            m_instructionDictionary.Add("BGTU", BGTU);

            m_instructionDictionary.Add("bleu", BLEU);
            m_instructionDictionary.Add("BLEU", BLEU);

            m_instructionDictionary.Add("j", J);
            m_instructionDictionary.Add("J", J);

            m_instructionDictionary.Add("jr", JR);
            m_instructionDictionary.Add("JR", JR);

            m_instructionDictionary.Add("ret", RET);
            m_instructionDictionary.Add("RET", RET);

            m_instructionDictionary.Add("call", CALL);
            m_instructionDictionary.Add("CALL", CALL);

            m_registerDictionary = new Dictionary<string, RVRegister>();

            m_registerDictionary.Add("x0", RVRegister.x0);
            m_registerDictionary.Add("X0", RVRegister.x0);

            m_registerDictionary.Add("x1", RVRegister.x1);
            m_registerDictionary.Add("X1", RVRegister.x1);

            m_registerDictionary.Add("x2", RVRegister.x2);
            m_registerDictionary.Add("X2", RVRegister.x2);

            m_registerDictionary.Add("x3", RVRegister.x3);
            m_registerDictionary.Add("X3", RVRegister.x3);

            m_registerDictionary.Add("x4", RVRegister.x4);
            m_registerDictionary.Add("X4", RVRegister.x4);

            m_registerDictionary.Add("x5", RVRegister.x5);
            m_registerDictionary.Add("X5", RVRegister.x5);

            m_registerDictionary.Add("x6", RVRegister.x6);
            m_registerDictionary.Add("X6", RVRegister.x6);

            m_registerDictionary.Add("x7", RVRegister.x7);
            m_registerDictionary.Add("X7", RVRegister.x7);

            m_registerDictionary.Add("x8", RVRegister.x8);
            m_registerDictionary.Add("X8", RVRegister.x8);

            m_registerDictionary.Add("x9", RVRegister.x9);
            m_registerDictionary.Add("X9", RVRegister.x9);

            m_registerDictionary.Add("x10", RVRegister.x10);
            m_registerDictionary.Add("X10", RVRegister.x10);

            m_registerDictionary.Add("x11", RVRegister.x11);
            m_registerDictionary.Add("X11", RVRegister.x11);

            m_registerDictionary.Add("x12", RVRegister.x12);
            m_registerDictionary.Add("X12", RVRegister.x12);

            m_registerDictionary.Add("x13", RVRegister.x13);
            m_registerDictionary.Add("X13", RVRegister.x13);

            m_registerDictionary.Add("x14", RVRegister.x14);
            m_registerDictionary.Add("X14", RVRegister.x14);

            m_registerDictionary.Add("x15", RVRegister.x15);
            m_registerDictionary.Add("X15", RVRegister.x15);

            m_registerDictionary.Add("x16", RVRegister.x16);
            m_registerDictionary.Add("X16", RVRegister.x16);

            m_registerDictionary.Add("x17", RVRegister.x17);
            m_registerDictionary.Add("X17", RVRegister.x17);

            m_registerDictionary.Add("x18", RVRegister.x18);
            m_registerDictionary.Add("X18", RVRegister.x18);

            m_registerDictionary.Add("x19", RVRegister.x19);
            m_registerDictionary.Add("X19", RVRegister.x19);

            m_registerDictionary.Add("x20", RVRegister.x20);
            m_registerDictionary.Add("X20", RVRegister.x20);

            m_registerDictionary.Add("x21", RVRegister.x21);
            m_registerDictionary.Add("X21", RVRegister.x21);

            m_registerDictionary.Add("x22", RVRegister.x22);
            m_registerDictionary.Add("X22", RVRegister.x22);

            m_registerDictionary.Add("x23", RVRegister.x23);
            m_registerDictionary.Add("X23", RVRegister.x23);

            m_registerDictionary.Add("x24", RVRegister.x24);
            m_registerDictionary.Add("X24", RVRegister.x24);

            m_registerDictionary.Add("x25", RVRegister.x25);
            m_registerDictionary.Add("X25", RVRegister.x25);

            m_registerDictionary.Add("x26", RVRegister.x26);
            m_registerDictionary.Add("X26", RVRegister.x26);

            m_registerDictionary.Add("x27", RVRegister.x27);
            m_registerDictionary.Add("X27", RVRegister.x27);

            m_registerDictionary.Add("x28", RVRegister.x28);
            m_registerDictionary.Add("X28", RVRegister.x28);

            m_registerDictionary.Add("x29", RVRegister.x29);
            m_registerDictionary.Add("X29", RVRegister.x29);

            m_registerDictionary.Add("x30", RVRegister.x30);
            m_registerDictionary.Add("X30", RVRegister.x30);

            m_registerDictionary.Add("x31", RVRegister.x31);
            m_registerDictionary.Add("X31", RVRegister.x31);

            m_registerDictionary.Add("zero", RVRegister.x0);
            m_registerDictionary.Add("ZERO", RVRegister.x0);

            m_registerDictionary.Add("ra", RVRegister.x1);
            m_registerDictionary.Add("RA", RVRegister.x1);

            m_registerDictionary.Add("sp", RVRegister.x2);
            m_registerDictionary.Add("SP", RVRegister.x2);

            m_registerDictionary.Add("gp", RVRegister.x3);
            m_registerDictionary.Add("GP", RVRegister.x3);

            m_registerDictionary.Add("tp", RVRegister.x4);
            m_registerDictionary.Add("TP", RVRegister.x4);

            m_registerDictionary.Add("fp", RVRegister.x8);
            m_registerDictionary.Add("FP", RVRegister.x8);

            m_registerDictionary.Add("t0", RVRegister.x5);
            m_registerDictionary.Add("T0", RVRegister.x5);

            m_registerDictionary.Add("t1", RVRegister.x6);
            m_registerDictionary.Add("T1", RVRegister.x6);

            m_registerDictionary.Add("t2", RVRegister.x7);
            m_registerDictionary.Add("T2", RVRegister.x7);

            m_registerDictionary.Add("t3", RVRegister.x28);
            m_registerDictionary.Add("T3", RVRegister.x28);

            m_registerDictionary.Add("t4", RVRegister.x29);
            m_registerDictionary.Add("T4", RVRegister.x29);

            m_registerDictionary.Add("t5", RVRegister.x30);
            m_registerDictionary.Add("T5", RVRegister.x30);

            m_registerDictionary.Add("t6", RVRegister.x31);
            m_registerDictionary.Add("T6", RVRegister.x31);

            m_registerDictionary.Add("s0", RVRegister.x8);
            m_registerDictionary.Add("S0", RVRegister.x8);

            m_registerDictionary.Add("s1", RVRegister.x9);
            m_registerDictionary.Add("S1", RVRegister.x9);

            m_registerDictionary.Add("s2", RVRegister.x18);
            m_registerDictionary.Add("S2", RVRegister.x18);

            m_registerDictionary.Add("s3", RVRegister.x19);
            m_registerDictionary.Add("S3", RVRegister.x19);

            m_registerDictionary.Add("s4", RVRegister.x20);
            m_registerDictionary.Add("S4", RVRegister.x20);

            m_registerDictionary.Add("s5", RVRegister.x21);
            m_registerDictionary.Add("S5", RVRegister.x21);

            m_registerDictionary.Add("s6", RVRegister.x22);
            m_registerDictionary.Add("S6", RVRegister.x22);

            m_registerDictionary.Add("s7", RVRegister.x23);
            m_registerDictionary.Add("S7", RVRegister.x23);

            m_registerDictionary.Add("s8", RVRegister.x24);
            m_registerDictionary.Add("S8", RVRegister.x24);

            m_registerDictionary.Add("s9", RVRegister.x25);
            m_registerDictionary.Add("S9", RVRegister.x25);

            m_registerDictionary.Add("s10", RVRegister.x26);
            m_registerDictionary.Add("S10", RVRegister.x26);

            m_registerDictionary.Add("s11", RVRegister.x27);
            m_registerDictionary.Add("S11", RVRegister.x27);

            m_registerDictionary.Add("a0", RVRegister.x10);
            m_registerDictionary.Add("A0", RVRegister.x10);

            m_registerDictionary.Add("a1", RVRegister.x11);
            m_registerDictionary.Add("A1", RVRegister.x11);

            m_registerDictionary.Add("a2", RVRegister.x12);
            m_registerDictionary.Add("A2", RVRegister.x12);

            m_registerDictionary.Add("a3", RVRegister.x13);
            m_registerDictionary.Add("A3", RVRegister.x13);

            m_registerDictionary.Add("a4", RVRegister.x14);
            m_registerDictionary.Add("A4", RVRegister.x14);

            m_registerDictionary.Add("a5", RVRegister.x15);
            m_registerDictionary.Add("A5", RVRegister.x15);

            m_registerDictionary.Add("a6", RVRegister.x16);
            m_registerDictionary.Add("A6", RVRegister.x16);

            m_registerDictionary.Add("a7", RVRegister.x17);
            m_registerDictionary.Add("A7", RVRegister.x17);

            m_bTypeInstructions = new Dictionary<byte, String>();

            m_bTypeInstructions.Add(0b000, "beq");
            m_bTypeInstructions.Add(0b001, "bne");
            m_bTypeInstructions.Add(0b100, "blt");
            m_bTypeInstructions.Add(0b101, "bge");
            m_bTypeInstructions.Add(0b110, "bltu");
            m_bTypeInstructions.Add(0b111, "bgeu");

            m_loadTypeInstructions = new Dictionary<byte, String>();

            m_loadTypeInstructions.Add(0b000, "lb");
            m_loadTypeInstructions.Add(0b001, "lh");
            m_loadTypeInstructions.Add(0b010, "lw");
            m_loadTypeInstructions.Add(0b100, "lbu");
            m_loadTypeInstructions.Add(0b101, "lhu");

            m_sTypeInstructions = new Dictionary<byte, String>();

            m_sTypeInstructions.Add(0b000, "sb");
            m_sTypeInstructions.Add(0b001, "sh");
            m_sTypeInstructions.Add(0b010, "sw");

            m_iTypeInstructions = new Dictionary<byte, String>();

            m_iTypeInstructions.Add(0b000, "addi");
            m_iTypeInstructions.Add(0b010, "slti");
            m_iTypeInstructions.Add(0b011, "sltiu");
            m_iTypeInstructions.Add(0b100, "xori");
            m_iTypeInstructions.Add(0b110, "ori");
            m_iTypeInstructions.Add(0b111, "andi");

            m_shiftTypeInstructions = new Dictionary<UInt16, String>();

            m_shiftTypeInstructions.Add(0b0000000001, "slli");
            m_shiftTypeInstructions.Add(0b0000000101, "srli");
            m_shiftTypeInstructions.Add(0b0100000101, "srai");

            m_rTypeInstructions = new Dictionary<UInt16, String>();

            m_rTypeInstructions.Add(0b0000000000, "add");
            m_rTypeInstructions.Add(0b0100000000, "sub");
            m_rTypeInstructions.Add(0b0000000001, "sll");
            m_rTypeInstructions.Add(0b0000000010, "slt");
            m_rTypeInstructions.Add(0b0000000011, "sltu");
            m_rTypeInstructions.Add(0b0000000100, "xor");
            m_rTypeInstructions.Add(0b0000000101, "srl");
            m_rTypeInstructions.Add(0b0100000101, "sra");
            m_rTypeInstructions.Add(0b0000000110, "or");
            m_rTypeInstructions.Add(0b0000000111, "and");
            m_rTypeInstructions.Add(0b0000001000, "mul");
            m_rTypeInstructions.Add(0b0000001001, "mulh");
            m_rTypeInstructions.Add(0b0000001010, "mulhsu");
            m_rTypeInstructions.Add(0b0000001011, "mulhu");
            m_rTypeInstructions.Add(0b0000001100, "div");
            m_rTypeInstructions.Add(0b0000001101, "divu");
            m_rTypeInstructions.Add(0b0000001110, "rem");
            m_rTypeInstructions.Add(0b0000001111, "remu");

            m_systemInstructions = new Dictionary<UInt16, String>();

            m_systemInstructions.Add(0x000, "ecall");
            m_systemInstructions.Add(0x001, "ebreak");
            m_systemInstructions.Add(0xFFF, "hlt");
            m_systemInstructions.Add(0xFFE, "rst");
            m_systemInstructions.Add(0x107, "ien");
            m_systemInstructions.Add(0x106, "idis");
            m_systemInstructions.Add(0x105, "wfi");
            m_systemInstructions.Add(0x102, "iret");
        }

        public static RVInstructionDescription? GetInstructionByString(String inst)
        {
            lock (s_lock) // multiple threads may compile/decompile at the same time, so lock is required here
            {
                if (s_instance == null)
                {
                    s_instance = new RVInstructions();
                }
            }

            RVInstructionDescription l_instruction;
            if (s_instance.m_instructionDictionary.TryGetValue(inst, out l_instruction))
            {
                return l_instruction;
            }

            return null;
        }

        public static RVRegister? GetRegisterByString(String reg)
        {
            lock (s_lock) // multiple threads may compile/decompile at the same time, so lock is required here
            {
                if (s_instance == null)
                {
                    s_instance = new RVInstructions();
                }
            }

            RVRegister l_register;
            if (s_instance.m_registerDictionary.TryGetValue(reg, out l_register))
            {
                return l_register;
            }

            return null;
        }

        public static Tuple<String, String> DisassembleIntruction(UInt32 encodedInstruction, RVLabelReferenceMap labelMap = null, UInt32 address = 0)
        {// returns a pair of strings (1st is the decoded instruction) (2nd is the split instruction bits)
            lock (s_lock) // multiple threads may compile/decompile at the same time, so lock is required here
            {
                if (s_instance == null)
                {
                    s_instance = new RVInstructions();
                }
            }

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

                    if (labelMap != null)
                    {
                        String[] l_labels = labelMap.FindByAddress(address + (UInt32)l_offset);
                        for (int i_labelIndex = 0; i_labelIndex < l_labels.Length; ++i_labelIndex)
                        {
                            l_assembly += i_labelIndex == 0 ? " [" : ", ";
                            l_assembly += l_labels[i_labelIndex];

                            if (i_labelIndex == l_labels.Length - 1)
                            {
                                l_assembly += "]";
                            }
                        }
                    }

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

                    if (s_instance.m_bTypeInstructions.TryGetValue(l_func3, out l_mnemonic) == false)
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

                    if (labelMap != null)
                    {
                        String[] l_labels = labelMap.FindByAddress((UInt32)address + (UInt32)l_offset);
                        for (int i_labelIndex = 0; i_labelIndex < l_labels.Length; ++i_labelIndex)
                        {
                            l_assembly += i_labelIndex == 0 ? " [" : ", ";
                            l_assembly += l_labels[i_labelIndex];

                            if (i_labelIndex == l_labels.Length - 1)
                            {
                                l_assembly += "]";
                            }
                        }
                    }

                    return new Tuple<String, String>(l_assembly, l_splitBits);
                }

                case 0b0000011: // loads
                {
                    byte l_func3 = (byte)((encodedInstruction >> 12) & 0x7);
                    String l_mnemonic;

                    if (s_instance.m_loadTypeInstructions.TryGetValue(l_func3, out l_mnemonic) == false)
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

                    if (s_instance.m_sTypeInstructions.TryGetValue(l_func3, out l_mnemonic) == false)
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

                    if (s_instance.m_iTypeInstructions.TryGetValue(l_func3, out l_mnemonic))
                    {
                        byte l_destinationRegister = (byte)((encodedInstruction >> 7) & 0x1F);
                        byte l_sourceRegister1 = (byte)((encodedInstruction >> 15) & 0x1F);
                        UInt32 l_immediate = encodedInstruction >> 20;
                        Int32 l_offset = ((Int32)l_immediate << (31 - 11)) >> (31 - 11);

                        String l_assembly = String.Format("{0} x{1}, x{2}, {3}", l_mnemonic, l_destinationRegister, l_sourceRegister1, l_offset);
                        String l_splitBits = Convert.ToString(l_immediate, 2).PadLeft(12, '0') + '_' + Convert.ToString(l_sourceRegister1, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_func3, 2).PadLeft(3, '0') + '_' + Convert.ToString(l_destinationRegister, 2).PadLeft(5, '0') + '_' + Convert.ToString(l_opcode, 2).PadLeft(7, '0');

                        return new Tuple<String, String>(l_assembly, l_splitBits);
                    }
                    else if (s_instance.m_shiftTypeInstructions.TryGetValue(l_extendedFunction, out l_mnemonic))
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

                    if (s_instance.m_rTypeInstructions.TryGetValue(l_extendedFunction, out l_mnemonic) == false)
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

                    if (s_instance.m_systemInstructions.TryGetValue(l_func12, out l_mnemonic) == false)
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

        public static RVInstructionDescription LUI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.U, size = 4, opcode = 0b0110111 };
            }
        }

        public static RVInstructionDescription AUIPC
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.U, size = 4, opcode = 0b0010111 };
            }
        }

        public static RVInstructionDescription JAL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.J, size = 4, opcode = 0b1101111 };
            }
        }

        public static RVInstructionDescription JALR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.IOffset, size = 4, opcode = 0b1100111, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription BEQ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, size = 4, opcode = 0b1100011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription BNE
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, size = 4, opcode = 0b1100011, func3 = 0b001 };
            }
        }

        public static RVInstructionDescription BLT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, size = 4, opcode = 0b1100011, func3 = 0b100 };
            }
        }

        public static RVInstructionDescription BGE
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, size = 4, opcode = 0b1100011, func3 = 0b101 };
            }
        }

        public static RVInstructionDescription BLTU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, size = 4, opcode = 0b1100011, func3 = 0b110 };
            }
        }

        public static RVInstructionDescription BGEU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, size = 4, opcode = 0b1100011, func3 = 0b111 };
            }
        }

        public static RVInstructionDescription LB
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.IOffset, size = 4, opcode = 0b0000011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription LH
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.IOffset, size = 4, opcode = 0b0000011, func3 = 0b001 };
            }
        }

        public static RVInstructionDescription LW
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.IOffset, size = 4, opcode = 0b0000011, func3 = 0b010 };
            }
        }

        public static RVInstructionDescription LBU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.IOffset, size = 4, opcode = 0b0000011, func3 = 0b100 };
            }
        }

        public static RVInstructionDescription LHU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.IOffset, size = 4, opcode = 0b0000011, func3 = 0b101 };
            }
        }

        public static RVInstructionDescription SB
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.S, size = 4, opcode = 0b0100011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription SH
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.S, size = 4, opcode = 0b0100011, func3 = 0b001 };
            }
        }

        public static RVInstructionDescription SW
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.S, size = 4, opcode = 0b0100011, func3 = 0b010 };
            }
        }

        public static RVInstructionDescription ADDI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, size = 4, opcode = 0b0010011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription SLTI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, size = 4, opcode = 0b0010011, func3 = 0b010 };
            }
        }

        public static RVInstructionDescription SLTIU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, size = 4, opcode = 0b0010011, func3 = 0b011 };
            }
        }

        public static RVInstructionDescription XORI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, size = 4, opcode = 0b0010011, func3 = 0b100 };
            }
        }

        public static RVInstructionDescription ORI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, size = 4, opcode = 0b0010011, func3 = 0b110 };
            }
        }

        public static RVInstructionDescription ANDI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, size = 4, opcode = 0b0010011, func3 = 0b111 };
            }
        }

        public static RVInstructionDescription SLLI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Shift, size = 4, opcode = 0b0010011, func3 = 0b001, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRLI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Shift, size = 4, opcode = 0b0010011, func3 = 0b101, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRAI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Shift, size = 4, opcode = 0b0010011, func3 = 0b101, func7 = 0b0100000 };
            }
        }

        public static RVInstructionDescription ADD
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b000, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SUB
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b000, func7 = 0b0100000 };
            }
        }

        public static RVInstructionDescription SLL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b001, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SLT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b010, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SLTU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b011, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription XOR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b100, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b101, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRA
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b101, func7 = 0b0100000 };
            }
        }

        public static RVInstructionDescription OR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b110, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription AND
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b111, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription ECALL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0x000 };
            }
        }

        public static RVInstructionDescription EBREAK
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0x001 };
            }
        }

        public static RVInstructionDescription MUL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b000, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription MULH
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b001, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription MULHSU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b010, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription MULHU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b011, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription DIV
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b100, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription DIVU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b101, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription REM
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b110, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription REMU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, size = 4, opcode = 0b0110011, func3 = 0b111, func7 = 0b0000001 };
            }
        }

        public static RVInstructionDescription HLT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0xFFF };
            }
        }

        public static RVInstructionDescription RST
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0xFFE };
            }
        }

        public static RVInstructionDescription IEN
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0x107 };
            }
        }

        public static RVInstructionDescription IDIS
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0x106 };
            }
        }

        public static RVInstructionDescription WFI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0x105 };
            }
        }

        public static RVInstructionDescription IRET
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, size = 4, opcode = 0b1110011, func3 = 0b000, func12 = 0x102 };
            }
        }

        public static RVInstructionDescription LA
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 8, mnemonic = "la" };
            }
        }

        public static RVInstructionDescription LI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 8, mnemonic = "li" };
            }
        }

        public static RVInstructionDescription NOP
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "nop" };
            }
        }

        public static RVInstructionDescription MV
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "mv" };
            }
        }

        public static RVInstructionDescription NOT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "not" };
            }
        }

        public static RVInstructionDescription NEG
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "neg" };
            }
        }

        public static RVInstructionDescription SEQZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "seqz" };
            }
        }

        public static RVInstructionDescription SNEZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "snez" };
            }
        }

        public static RVInstructionDescription SLTZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "sltz" };
            }
        }

        public static RVInstructionDescription SGTZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "sgtz" };
            }
        }

        public static RVInstructionDescription BEQZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "beqz" };
            }
        }

        public static RVInstructionDescription BNEZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bnez" };
            }
        }

        public static RVInstructionDescription BLEZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "blez" };
            }
        }

        public static RVInstructionDescription BGEZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bgez" };
            }
        }

        public static RVInstructionDescription BLTZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bltz" };
            }
        }

        public static RVInstructionDescription BGTZ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bgtz" };
            }
        }

        public static RVInstructionDescription BGT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bgt" };
            }
        }

        public static RVInstructionDescription BLE
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "ble" };
            }
        }

        public static RVInstructionDescription BGTU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bgtu" };
            }
        }

        public static RVInstructionDescription BLEU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "bleu" };
            }
        }

        public static RVInstructionDescription J
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "j" };
            }
        }

        public static RVInstructionDescription JR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "jr" };
            }
        }

        public static RVInstructionDescription RET
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 4, mnemonic = "ret" };
            }
        }

        public static RVInstructionDescription CALL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Pseudo, size = 8, mnemonic = "call" };
            }
        }
    }
}
