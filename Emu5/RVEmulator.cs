using System;
using System.Collections.Generic;

namespace Emu5
{
    class RVEmulator
    {
       RVMemoryMap m_memoryMap;

        public RVEmulator()
        {
            m_memoryMap = new RVMemoryMap();
        }

        public void Assemble(String code)
        {
            RVAssembler.Assemble(code, m_memoryMap);
        }
    }
}
