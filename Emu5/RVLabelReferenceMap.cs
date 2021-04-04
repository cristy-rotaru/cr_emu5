using System;
using System.Collections.Generic;

namespace Emu5
{
    public class RVLabelReferenceMap
    {
        List<Tuple<UInt32, String>> m_entryList;

        public RVLabelReferenceMap()
        {
            m_entryList = new List<Tuple<UInt32, String>>();
        }

        public void Add(UInt32 address, String label)
        { // items will be added in sorted order to ease search
            int l_addIndex = 0;
            for (; l_addIndex < m_entryList.Count; ++l_addIndex)
            {
                if (m_entryList[l_addIndex].Item1 > address)
                {
                    break; // we found the location
                }
            }

            m_entryList.Insert(l_addIndex, new Tuple<UInt32, String>(address, label));
        }

        public void Clear()
        {
            m_entryList.Clear();
        }

        public String[] FindByAddress(UInt32 address)
        {
            if (m_entryList.Count == 0)
            {
                return new String[0];
            }

            int l_lastIndex = m_entryList.Count - 1;
            if (address < m_entryList[0].Item1 || address > m_entryList[l_lastIndex].Item1)
            {
                return new String[0];
            }

            List<String> l_labelList = new List<String>();
            foreach (Tuple<UInt32, String> i_entry in m_entryList)
            {
                if (i_entry.Item1 == address)
                {
                    l_labelList.Add(i_entry.Item2);
                }
                else if (i_entry.Item1 > address)
                {
                    break;
                }
            }

            return l_labelList.ToArray();
        }
    }
}
