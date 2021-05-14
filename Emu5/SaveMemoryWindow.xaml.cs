using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for SaveMemoryWindow.xaml
    /// </summary>
    public partial class SaveMemoryWindow : Window
    {
        RVMemoryMap m_memoryMap;
        String m_documentsPath;
        String m_initialFileName;

        public SaveMemoryWindow(RVMemoryMap memoryMap, String fileName)
        {
            InitializeComponent();

            m_memoryMap = memoryMap;
            m_documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (fileName == null)
            {
                m_initialFileName = "memory_chunk.hex";
            }
            else
            {
                m_initialFileName = fileName;
                if (m_initialFileName.Contains("."))
                {
                    int l_lastPointIndex = -1;
                    for (int i_charIndex = m_initialFileName.Length - 1; i_charIndex >= 0; --i_charIndex)
                    {
                        if (m_initialFileName[i_charIndex] == '.')
                        {
                            l_lastPointIndex = i_charIndex;
                            break;
                        }
                    }
                    if (l_lastPointIndex >= 0)
                    {
                        m_initialFileName = m_initialFileName.Substring(0, l_lastPointIndex);
                    }
                }
                m_initialFileName += ".hex";
            }

            textBoxFileName.Text = m_documentsPath + '\\' + m_initialFileName;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            int l_lastSlashPosition = -1; // invalid position
            for (int i_charPosition = textBoxFileName.Text.Length - 1; i_charPosition >= 0; --i_charPosition)
            {
                if (textBoxFileName.Text[i_charPosition] == '\\')
                {
                    l_lastSlashPosition = i_charPosition;
                    break;
                }
            }

            String l_filePath = null;
            String l_fileName = null;

            if (l_lastSlashPosition >= 0)
            {
                l_filePath = textBoxFileName.Text.Substring(0, l_lastSlashPosition);
                l_fileName = textBoxFileName.Text.Substring(l_lastSlashPosition + 1);
            }

            SaveFileDialog l_saveFileDialog = new SaveFileDialog();
            l_saveFileDialog.Filter = "Binary files (*.hex, *.bin)|*.hex;*.bin|All files|*";

            if (String.IsNullOrEmpty(l_filePath) == false)
            {
                l_saveFileDialog.InitialDirectory = l_filePath;
            }

            if (String.IsNullOrWhiteSpace(l_fileName))
            {
                l_saveFileDialog.FileName = m_initialFileName;
            }
            else
            {
                l_saveFileDialog.FileName = l_fileName;
            }

            if (l_saveFileDialog.ShowDialog() == true)
            {
                textBoxFileName.Text = l_saveFileDialog.FileName;
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            UInt32 l_startAddress, l_endAddress;
            try
            {
                l_startAddress = UInt32.Parse(textBoxStartAddress.Text, NumberStyles.HexNumber);
            }
            catch (Exception e_conversionError)
            {
                MessageBox.Show(e_conversionError.Message, "Invalid start address", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                l_endAddress = UInt32.Parse(textBoxEndAddress.Text, NumberStyles.HexNumber);
            }
            catch (Exception e_conversionError)
            {
                MessageBox.Show(e_conversionError.Message, "Invalid end address", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            

            UInt64 l_delta = ((UInt64)(l_endAddress - l_startAddress) & 0xFFFFFFFF) + 1;

            if (l_delta > 1073741824) // set limit to 1GB
            {
                MessageBox.Show("The selected range exceeds the 1GB limit.", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool l_continue = true;
            if (l_delta >= 67108864) // 64 MB
            {
                l_continue = false;

                double l_divider = l_delta > 1130254551 ? 1073741824.0 : 1048576.0;
                String l_unit = l_delta > 1130254551 ? "GB" : "MB"; // about .95 GB

                String l_message = "You are trying to save a large file (" + (l_delta / l_divider).ToString("n2") + ' ' + l_unit + ")\n";
                l_message += "Are you sure you want to continue?";

                if (MessageBox.Show(l_message, "Large file warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    l_continue = true;
                }
            }

            if (l_continue == false)
            {
                return;
            }

            try
            {
                BinaryWriter l_fileWriter = new BinaryWriter(File.Open(textBoxFileName.Text, FileMode.OpenOrCreate, FileAccess.Write));

                byte?[] l_memoryData = m_memoryMap.ReadIgnorePeripherals(l_startAddress, (int)l_delta);
                byte[] l_saveData = new byte[l_memoryData.Length];
                for (int i_byteIndex = 0; i_byteIndex < l_memoryData.Length; ++i_byteIndex)
                {
                    l_saveData[i_byteIndex] = l_memoryData[i_byteIndex] != null ? (byte)l_memoryData[i_byteIndex] : (byte)0x00;
                }

                l_fileWriter.Write(l_saveData);
                l_fileWriter.Close();

                this.Close();
            }
            catch (Exception e_saveError)
            {
                MessageBox.Show(e_saveError.Message, "Error saving file", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
