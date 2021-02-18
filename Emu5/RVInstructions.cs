using System;
using System.Collections.Generic;

namespace Emu5
{
    enum RVSubSet
    {
        RV32I,
        RV64I,
        ZifenceI,
        ZiCSR,
        RV32M,
        RV64M,
        RV32A,
        RV64A,
        RV32F,
        RV64F,
        RV32D,
        RV64D,
        RV32Q,
        RV64Q
    }

    enum RVInstructionType
    {
        R,
        I,
        S,
        B,
        U,
        J,
        Fence,
        System,
        Pseudo
    }

    enum RVRegister
    {
        x0 = 0,
        x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15, x16, x17, x18, x19, x20, x21, x22, x23, x24, x25, x26, x27, x28, x29, x30, x31,
        f0 = 0x20,
        f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15, f16, f17, f18, f19, f20, f21, f22, f23, f24, f25, f26, f27, f28, f29, f30, f31
    }

    enum RVCSR
    {
        fflags = 0x001,
        frm = 0x002,
        fcsr = 0x003,
        cycle = 0xC00,
        time = 0xC01,
        instret = 0xC02
    }

    struct RVInstructionDescription
    {
        public RVSubSet subSet;
        public RVInstructionType type;
        public byte opcode;
        public byte func3;
        public byte func7;
        public UInt32 imm;
        public RVRegister rs1;
        public RVRegister rs2;
        public RVRegister rs3;
        public RVRegister rd;
        public RVCSR csr;
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
                return new RVInstructionDescription { subSet = RVSubSet.RV32I, type = RVInstructionType.U, opcode = 0b0110111 };
            }
        }

        public static RVInstructionDescription AUIPC
        {
            get
            {
                return new RVInstructionDescription { subSet = RVSubSet.RV32I, type = RVInstructionType.U, opcode = 0b0010111 };
            }
        }

        public static RVInstructionDescription JAL
        {
            get
            {
                return new RVInstructionDescription { subSet = RVSubSet.RV32I, type = RVInstructionType.J, opcode = 0b1101111 };
            }
        }

        public static RVInstructionDescription JALR
        {
            get
            {
                return new RVInstructionDescription { subSet = RVSubSet.RV32I, type = RVInstructionType.I, opcode = 0b1100111, func3 = 0b000 };
            }
        }
    }
}
