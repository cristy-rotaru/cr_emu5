using System;

namespace Emu5
{
    class RVEmulationException : Exception
    {
        public RVEmulationException(String message) : base(message) { }
    }

    class RVEmulator
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
    }
}
