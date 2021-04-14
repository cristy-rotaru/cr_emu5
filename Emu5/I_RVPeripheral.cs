﻿using System;

namespace Emu5
{
    public interface I_RVPeripheral
    {
        byte[] ReadRegisters(UInt32 offset, int count);
        void WriteRegisters(UInt32 offset, byte[] data);
    }
}
