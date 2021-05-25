using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for MemoryMapSettingsPanel.xaml
    /// </summary>
    public partial class MemoryMapSettingsPanel : UserControl
    {
        List<Interval> m_memoryRanges;

        public MemoryMapSettingsPanel()
        {
            InitializeComponent();

            m_memoryRanges = new List<Interval>();
        }

        public void SetMemoryRanges(List<Interval> ranges)
        {
            m_memoryRanges.Clear();

            if (CheckRangesIntegrity(ranges))
            {
                foreach (Interval i_interval in ranges)
                {
                    m_memoryRanges.Add(i_interval);
                }
            }

            UpdateMemoryRangesUI();
        }

        public void SetDefaultMemoryValue(byte defaultValue)
        {
            textBoxDefaultMemoryValue.Text = String.Format("{0,2:X2}", defaultValue);
        }

        public List<Interval> GetMemoryRanges()
        {
            return new List<Interval>(m_memoryRanges);
        }

        public byte? GetDefaultMemoryValue()
        {
            try
            {
                byte l_value = byte.Parse(textBoxDefaultMemoryValue.Text, NumberStyles.HexNumber);
                return l_value;
            }
            catch
            {
                return null;
            }
        }

        private bool CheckRangesIntegrity(List<Interval> ranges)
        {
            for (int i_indexBase = 0; i_indexBase < ranges.Count; ++i_indexBase)
            {
                for (int i_indexCheck = 0; i_indexCheck < ranges.Count; ++i_indexCheck)
                {
                    if (i_indexBase == i_indexCheck)
                    {
                        continue;
                    }

                    if (ranges[i_indexCheck].start >= ranges[i_indexBase].start && ranges[i_indexCheck].start <= ranges[i_indexBase].end)
                    {
                        return false;
                    }
                    if (ranges[i_indexCheck].end >= ranges[i_indexBase].start && ranges[i_indexCheck].end <= ranges[i_indexBase].end)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void buttonAddMemoryRange_Click(object sender, RoutedEventArgs e)
        {
            UInt32 l_rangeBegin, l_rangeEnd;

            try
            {
                l_rangeBegin = UInt32.Parse(textBoxRangeBegin.Text, NumberStyles.HexNumber);
                l_rangeEnd = UInt32.Parse(textBoxRangeEnd.Text, NumberStyles.HexNumber);
            }
            catch
            {
                MessageBox.Show("Range begin and range end must be in hex format.", "Incorrect number format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (l_rangeBegin >= l_rangeEnd)
            {
                MessageBox.Show("Range end must be greater than range begin.", "Invalid range", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            Interval l_newInterval = new Interval { start = l_rangeBegin, end = l_rangeEnd };

            // check for overlapping
            foreach (Interval i_interval in m_memoryRanges)
            {
                if ((l_newInterval.start >= i_interval.start && l_newInterval.start <= i_interval.end) || (l_newInterval.start >= i_interval.start && l_newInterval.start <= i_interval.end))
                { // interval overlaps
                    MessageBox.Show("The new range overlaps with an existing one.", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // check if ranges can be merged
            bool l_merged = false;
            for (int i_index = 0; i_index < m_memoryRanges.Count; ++i_index)
            {
                if ((l_newInterval.start != 0) && m_memoryRanges[i_index].end + 1 == l_newInterval.start)
                { // can merge at the end
                    l_newInterval.start = m_memoryRanges[i_index].start;
                    m_memoryRanges.RemoveAt(i_index);
                    l_merged = true;
                    break;
                }

                if ((l_newInterval.end != 0xFFFFFFFF) && l_newInterval.end + 1 == m_memoryRanges[i_index].start)
                { // can merge at the beginning
                    l_newInterval.end = m_memoryRanges[i_index].end;
                    m_memoryRanges.RemoveAt(i_index);
                    l_merged = true;
                    break;
                }
            }

            // check if can merge even more
            if (l_merged)
            {
                for (int i_index = 0; i_index < m_memoryRanges.Count; ++i_index)
                {
                    if ((l_newInterval.start != 0) && m_memoryRanges[i_index].end + 1 == l_newInterval.start)
                    { // can merge at the end
                        l_newInterval.start = m_memoryRanges[i_index].start;
                        m_memoryRanges.RemoveAt(i_index);
                        break;
                    }

                    if ((l_newInterval.end != 0xFFFFFFFF) && l_newInterval.end + 1 == m_memoryRanges[i_index].start)
                    { // can merge at the beginning
                        l_newInterval.end = m_memoryRanges[i_index].end;
                        m_memoryRanges.RemoveAt(i_index);
                        break;
                    }
                }
            }

            m_memoryRanges.Add(l_newInterval);
            UpdateMemoryRangesUI();
        }

        private void buttonRemoveMemoryRange_Click(object sender, RoutedEventArgs e)
        {
            m_memoryRanges.RemoveAt(listBoxMemoryRanges.SelectedIndex);

            UpdateMemoryRangesUI();
        }

        private void UpdateMemoryRangesUI()
        {
            listBoxMemoryRanges.Items.Clear();

            if (m_memoryRanges.Count != 0)
            {
                foreach (Interval i_interval in m_memoryRanges)
                {
                    listBoxMemoryRanges.Items.Add(String.Format("0x{0,8:X8} - 0x{1,8:X8}", i_interval.start, i_interval.end));
                }

                listBoxMemoryRanges.SelectedIndex = 0;
                buttonRemoveMemoryRange.IsEnabled = true;
            }
            else
            {
                buttonRemoveMemoryRange.IsEnabled = false;
            }

            textBoxRangeBegin.Clear();
            textBoxRangeEnd.Clear();
        }
    }
}
