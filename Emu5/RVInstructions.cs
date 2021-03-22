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
        }

        public static RVInstructionDescription? GetInstructionByString(String inst)
        {
            lock (s_lock) // multiple threads may compile at the same time, so lock is required here
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
            lock (s_lock) // multiple threads may compile at the same time, so lock is required here
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
    }
}
