using System;
using System.Collections.Generic;

namespace Emu5
{
    enum RVDataType
    {
        DB, DH, DW, STR, STRZ
    }

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
        private static object s_lock = new object();
        private static RVAssembler s_instance;

        Dictionary<String, RVDataType> m_dataTypeDictionary;

        private RVAssembler()
        {
            m_dataTypeDictionary = new Dictionary<string, RVDataType>();

            m_dataTypeDictionary.Add("db", RVDataType.DB);
            m_dataTypeDictionary.Add("DB", RVDataType.DB);

            m_dataTypeDictionary.Add("dh", RVDataType.DH);
            m_dataTypeDictionary.Add("DH", RVDataType.DH);

            m_dataTypeDictionary.Add("dw", RVDataType.DW);
            m_dataTypeDictionary.Add("DW", RVDataType.DW);

            m_dataTypeDictionary.Add("str", RVDataType.STR);
            m_dataTypeDictionary.Add("STR", RVDataType.STR);

            m_dataTypeDictionary.Add("strz", RVDataType.STRZ);
            m_dataTypeDictionary.Add("STRZ", RVDataType.STRZ);
        }

        static public void Assemble(String code, Dictionary<UInt32, UInt64> memoryMap)
        {
            RVToken[][] l_tokens = RVParser.Tokenize(code);
        }

        static public RVDataType? GetDataTypeByString(String data)
        {
            lock (s_lock)
            {
                if (s_instance == null)
                {
                    s_instance = new RVAssembler();
                }
            }

            RVDataType l_dataType;
            if (s_instance.m_dataTypeDictionary.TryGetValue(data, out l_dataType))
            {
                return l_dataType;
            }

            return null;
        }
    }
}
