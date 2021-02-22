using System;
using System.Collections.Generic;

namespace Emu5
{
    class RVEmulator
    {
        Dictionary<UInt32, UInt64> m_memoryMap; // mapped on 8 bytes to reduce memory consumption

        public RVEmulator()
        {
            m_memoryMap = new Dictionary<UInt32, UInt64>();
        }

        public void Assemble(String code)
        {
            RVAssembler.Assemble(code, m_memoryMap);
        }
    }
}
