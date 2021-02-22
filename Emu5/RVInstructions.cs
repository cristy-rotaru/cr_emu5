using System;
using System.Collections.Generic;

namespace Emu5
{
    enum RVInstructionType
    {
        R,
        I,
        S,
        B,
        U,
        J,
        Load,
        Shift,
        System,
        Pseudo
    }

    enum RVRegister
    {
        x0 = 0,
        x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15, x16, x17, x18, x19, x20, x21, x22, x23, x24, x25, x26, x27, x28, x29, x30, x31
    }

    struct RVInstructionDescription
    {
        public RVInstructionType type;
        public byte opcode;
        public byte func3;
        public byte func7;
        public short func12;
        public byte shamt;
        public UInt32 imm;
        public RVRegister rs1;
        public RVRegister rs2;
        public RVRegister rs3;
        public RVRegister rd;
    }

    class RVInstructions
    {
        private RVInstructions()
        {

        }

        public static RVInstructionDescription LUI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.U, opcode = 0b0110111 };
            }
        }

        public static RVInstructionDescription AUIPC
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.U, opcode = 0b0010111 };
            }
        }

        public static RVInstructionDescription JAL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.J, opcode = 0b1101111 };
            }
        }

        public static RVInstructionDescription JALR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b1100111, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription BEQ
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, opcode = 0b1100011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription BNE
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, opcode = 0b1100011, func3 = 0b001 };
            }
        }

        public static RVInstructionDescription BLT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, opcode = 0b1100011, func3 = 0b100 };
            }
        }

        public static RVInstructionDescription BGE
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, opcode = 0b1100011, func3 = 0b101 };
            }
        }

        public static RVInstructionDescription BLTU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, opcode = 0b1100011, func3 = 0b110 };
            }
        }

        public static RVInstructionDescription BGEU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.B, opcode = 0b1100011, func3 = 0b111 };
            }
        }

        public static RVInstructionDescription LB
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Load, opcode = 0b0000011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription LH
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Load, opcode = 0b0000011, func3 = 0b001 };
            }
        }

        public static RVInstructionDescription LW
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Load, opcode = 0b0000011, func3 = 0b010 };
            }
        }

        public static RVInstructionDescription LBU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Load, opcode = 0b0000011, func3 = 0b100 };
            }
        }

        public static RVInstructionDescription LHU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Load, opcode = 0b0000011, func3 = 0b101 };
            }
        }

        public static RVInstructionDescription SB
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.S, opcode = 0b0100011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription SH
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.S, opcode = 0b0100011, func3 = 0b001 };
            }
        }

        public static RVInstructionDescription SW
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.S, opcode = 0b0100011, func3 = 0b010 };
            }
        }

        public static RVInstructionDescription ADDI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b0010011, func3 = 0b000 };
            }
        }

        public static RVInstructionDescription SLTI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b0010011, func3 = 0b010 };
            }
        }

        public static RVInstructionDescription SLTIU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b0010011, func3 = 0b011 };
            }
        }

        public static RVInstructionDescription XORI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b0010011, func3 = 0b100 };
            }
        }

        public static RVInstructionDescription ORI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b0010011, func3 = 0b110 };
            }
        }

        public static RVInstructionDescription ANDI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.I, opcode = 0b0010011, func3 = 0b111 };
            }
        }

        public static RVInstructionDescription SLLI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Shift, opcode = 0b0010011, func3 = 0b001, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRLI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Shift, opcode = 0b0010011, func3 = 0b101, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRAI
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.Shift, opcode = 0b0010011, func3 = 0b101, func7 = 0b0100000 };
            }
        }

        public static RVInstructionDescription ADD
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b000, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SUB
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b000, func7 = 0b0100000 };
            }
        }

        public static RVInstructionDescription SLL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b001, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SLT
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b010, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SLTU
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b011, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription XOR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b100, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b101, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription SRA
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b101, func7 = 0b0100000 };
            }
        }

        public static RVInstructionDescription OR
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b110, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription AND
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.R, opcode = 0b0110011, func3 = 0b111, func7 = 0b0000000 };
            }
        }

        public static RVInstructionDescription ECALL
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, opcode = 0b1110011, func3 = 0b000, func12 = 0x000 };
            }
        }

        public static RVInstructionDescription EBREAK
        {
            get
            {
                return new RVInstructionDescription { type = RVInstructionType.System, opcode = 0b1110011, func3 = 0b000, func12 = 0x001 };
            }
        }
    }
}
