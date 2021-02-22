using System;
using System.Collections.Generic;

namespace Emu5
{
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

    class RVAssembler
    {
        private RVAssembler() { }

        static public void Assemble(String code, Dictionary<UInt32, UInt64> memoryMap)
        {
            RVToken[][] l_tokens = RVParser.Tokenize(code);
        }
    }
}
