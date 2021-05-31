using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        bool m_simulationsAreRunning;

        EditorSettingsPanel m_editorSettings;
        EmulatorSettingsPanel m_emulatorSettings;
        MemoryMapSettingsPanel m_memoryMapSettings;
        PeripheralsSettingsPanel m_peripheralsSettings;
        TerminalSettingsPanel m_terminalSettings;
        LogSettingsPanel m_logSettings;

        int m_editorFontSize;
        bool m_editorEnableHighlighting;
        ProgramTemplate m_editorDefaultTemplate;

        bool m_emulatorClearMemoryMap;
        bool m_emulatorUseIntegratedEcallHandler;
        UInt32 m_emulatorEcallBase;

        List<Interval> m_memoryMapMemoryRanges;
        byte m_memoryMapUninitializedMemoryValue;

        bool m_peripheralsEnableIOPanel;
        bool m_peripheralsEnableTerminal;
        bool m_peripheralsEnableInterruptInjector;

        int m_terminalTextColorIndex;
        int m_terminalBackgroundColorIndex;

        bool m_loggingEnable;
        Verbosity m_loggingVerbosity;
        bool m_loggingClearOnNewSimulation;
        bool m_loggingDontLogEcall;

        public SettingsWindow(bool simulationsRunning = false)
        {
            InitializeComponent();

            m_simulationsAreRunning = simulationsRunning;

            m_editorSettings = new EditorSettingsPanel();
            m_emulatorSettings = new EmulatorSettingsPanel();
            m_memoryMapSettings = new MemoryMapSettingsPanel();
            m_peripheralsSettings = new PeripheralsSettingsPanel();
            m_terminalSettings = new TerminalSettingsPanel();
            m_logSettings = new LogSettingsPanel();

            CheckSettingsValidity();

            LoadSettings();
            treeViewItemEditor.IsSelected = true;
        }

        private void CheckSettingsValidity()
        {
            int l_fontSize = Properties.Settings.Default.editor_fontSize;
            if (l_fontSize < 6 || l_fontSize > 48)
            {
                ResetSettingsToDefault();
                return;
            }

            int l_programTemplate = Properties.Settings.Default.editor_defaultTemplate;
            if (l_programTemplate < 0 || l_programTemplate > 2)
            {
                ResetSettingsToDefault();
                return;
            }

            UInt32[] l_memoryRanges = Properties.Settings.Default.memoryMap_memoryRanges;
            if (l_memoryRanges == null || l_memoryRanges.Length == 0 || (l_memoryRanges.Length & 1) != 0)
            {
                ResetSettingsToDefault();
                return;
            }
            bool l_mainRangeFound = false;
            for (int i_rangeIndex = 0; i_rangeIndex < l_memoryRanges.Length; i_rangeIndex += 2)
            {
                if (l_memoryRanges[i_rangeIndex] >= l_memoryRanges[i_rangeIndex + 1])
                {
                    ResetSettingsToDefault();
                    return;
                }
                if (l_memoryRanges[i_rangeIndex] == 0x00000000 && l_memoryRanges[i_rangeIndex + 1] >= 0x80000)
                {
                    l_mainRangeFound = true;
                }
            }
            if (l_mainRangeFound == false)
            {
                ResetSettingsToDefault();
                return;
            }

            if (Properties.Settings.Default.emulator_useIntegratedEcallHandler)
            {
                UInt32 l_ecallBase = Properties.Settings.Default.emulator_ecallBase;
                if (l_ecallBase < 0x10000000 || (l_ecallBase & 0x3) != 0)
                {
                    ResetSettingsToDefault();
                    return;
                }
                MemoryStream l_ecallHandlerStream = new MemoryStream(Properties.Resources.ecall_handler);
                UInt32 l_ecallEnd = l_ecallBase + (UInt32)l_ecallHandlerStream.Length - 1;
                bool l_ecallRangeDefined = false;
                for (int i_rangeIndex = 0; i_rangeIndex < l_memoryRanges.Length; i_rangeIndex += 2)
                {
                    if (l_memoryRanges[i_rangeIndex] <= l_ecallBase && l_memoryRanges[i_rangeIndex + 1] >= l_ecallEnd)
                    {
                        l_ecallRangeDefined = true;
                    }
                }
                if (l_ecallRangeDefined == false)
                {
                    ResetSettingsToDefault();
                    return;
                }
                if (Properties.Settings.Default.peripherals_enableTerminal == false)
                {
                    ResetSettingsToDefault();
                    return;
                }
            }

            int l_verbosity = Properties.Settings.Default.logging_verbosity;
            if (l_verbosity < 0 || l_verbosity > 2)
            {
                ResetSettingsToDefault();
                return;
            }

            if (Properties.Settings.Default.logging_enable)
            {
                if (Properties.Settings.Default.logging_dontLogEcall == true && Properties.Settings.Default.emulator_useIntegratedEcallHandler == false)
                {
                    ResetSettingsToDefault();
                    return;
                }
            }
        }

        private void ResetSettingsToDefault()
        {
            MessageBox.Show("Invalid settings detected.\nResettings all settings to default.", "Settings corrupted", MessageBoxButton.OK, MessageBoxImage.Warning);

            Properties.Settings.Default.editor_fontSize = 15;
            Properties.Settings.Default.editor_enableHighlighting = true;
            Properties.Settings.Default.editor_defaultTemplate = 1;

            Properties.Settings.Default.emulator_clearMemoryMap = false;
            Properties.Settings.Default.emulator_useIntegratedEcallHandler = true;
            Properties.Settings.Default.emulator_ecallBase = 0xFFFFF000;

            UInt32[] l_memoryRanges = new UInt32[4];
            l_memoryRanges[0] = 0x00000000;
            l_memoryRanges[1] = 0x9FFFFFFF;
            l_memoryRanges[2] = 0xC0000000;
            l_memoryRanges[3] = 0xFFFFFFFF;
            Properties.Settings.Default.memoryMap_memoryRanges = l_memoryRanges;
            Properties.Settings.Default.memoryMap_uninitializedMemoryValue = 0xFF;

            Properties.Settings.Default.peripherals_enableIOPanel = true;
            Properties.Settings.Default.peripherals_enableTerminal = true;
            Properties.Settings.Default.peripherals_enableInterruptInjector = false;

            Properties.Settings.Default.terminal_textColorIndex = 21;
            Properties.Settings.Default.terminal_backgroundColorIndex = 0;

            Properties.Settings.Default.logging_enable = true;
            Properties.Settings.Default.logging_verbosity = 1;
            Properties.Settings.Default.logging_clearOnNewSimulation = false;
            Properties.Settings.Default.logging_dontLogEcall = true;

            Properties.Settings.Default.Save();
        }

        public void LoadSettings()
        { // at this point we know that all settings are valid
            m_editorFontSize = Properties.Settings.Default.editor_fontSize;
            m_editorEnableHighlighting = Properties.Settings.Default.editor_enableHighlighting;
            m_editorDefaultTemplate = (ProgramTemplate)Properties.Settings.Default.editor_defaultTemplate;

            m_editorSettings.SetFontSize(m_editorFontSize);
            m_editorSettings.SetSyntaxHighlighting(m_editorEnableHighlighting);
            m_editorSettings.SetNewFileTemplate(m_editorDefaultTemplate);

            m_emulatorClearMemoryMap = Properties.Settings.Default.emulator_clearMemoryMap;
            m_emulatorUseIntegratedEcallHandler = Properties.Settings.Default.emulator_useIntegratedEcallHandler;
            m_emulatorEcallBase = (UInt32)(Properties.Settings.Default.emulator_ecallBase & ~0x3);

            m_emulatorSettings.SetClearMemoryMap(m_emulatorClearMemoryMap);
            m_emulatorSettings.SetUseIntegratedEcall(m_emulatorUseIntegratedEcallHandler);
            m_emulatorSettings.SetEcallBase(m_emulatorEcallBase);

            m_memoryMapMemoryRanges = new List<Interval>();
            UInt32[] l_memoryRanges = Properties.Settings.Default.memoryMap_memoryRanges;
            for (int i_rangeIndex = 0; i_rangeIndex < l_memoryRanges.Length; i_rangeIndex += 2)
            {
                m_memoryMapMemoryRanges.Add(new Interval { start = l_memoryRanges[i_rangeIndex], end = l_memoryRanges[i_rangeIndex + 1] });
            }
            m_memoryMapUninitializedMemoryValue = Properties.Settings.Default.memoryMap_uninitializedMemoryValue;

            m_memoryMapSettings.SetMemoryRanges(m_memoryMapMemoryRanges);
            m_memoryMapSettings.SetDefaultMemoryValue(m_memoryMapUninitializedMemoryValue);

            m_peripheralsEnableIOPanel = Properties.Settings.Default.peripherals_enableIOPanel;
            m_peripheralsEnableTerminal = Properties.Settings.Default.peripherals_enableTerminal;
            m_peripheralsEnableInterruptInjector = Properties.Settings.Default.peripherals_enableInterruptInjector;

            m_peripheralsSettings.SetIOPanelEnabled(m_peripheralsEnableIOPanel);
            m_peripheralsSettings.SetTerminalEnabled(m_peripheralsEnableTerminal);
            m_peripheralsSettings.SetInterruptInjectorEnabled(m_peripheralsEnableInterruptInjector);

            m_terminalTextColorIndex = Properties.Settings.Default.terminal_textColorIndex;
            m_terminalBackgroundColorIndex = Properties.Settings.Default.terminal_backgroundColorIndex;

            m_terminalSettings.SetTextColorIndex(m_terminalTextColorIndex);
            m_terminalSettings.SetBackgroundColorIndex(m_terminalBackgroundColorIndex);

            m_loggingEnable = Properties.Settings.Default.logging_enable;
            m_loggingVerbosity = (Verbosity)Properties.Settings.Default.logging_verbosity;
            m_loggingClearOnNewSimulation = Properties.Settings.Default.logging_clearOnNewSimulation;
            m_loggingDontLogEcall = Properties.Settings.Default.logging_dontLogEcall;

            m_logSettings.SetLoggingEnabled(m_loggingEnable);
            m_logSettings.SetVerbosityLevel(m_loggingVerbosity);
            m_logSettings.SetClearLogOnNewSimulation(m_loggingClearOnNewSimulation);
            m_logSettings.SetEcallLoggingDisable(m_loggingDontLogEcall);
        }

        private bool SettingsChanged()
        {
            if (m_editorSettings.GetFontSize() != m_editorFontSize)
            {
                return true;
            }
            if (m_editorSettings.GetSyntaxHighlightingEnable() != m_editorEnableHighlighting)
            {
                return true;
            }
            if (m_editorSettings.GetNewFileTemplate() != m_editorDefaultTemplate)
            {
                return true;
            }

            if (m_emulatorSettings.GetClearMemoryMap() != m_emulatorClearMemoryMap)
            {
                return true;
            }
            if (m_emulatorSettings.GetUseIntegratedEcall() != m_emulatorUseIntegratedEcallHandler)
            {
                return true;
            }
            if (m_emulatorSettings.GetEcallBase() != m_emulatorEcallBase)
            {
                return true;
            }

            List<Interval> l_memoryRanges = m_memoryMapSettings.GetMemoryRanges();
            if (l_memoryRanges.Count != m_memoryMapMemoryRanges.Count)
            {
                return true;
            }
            for (int i_rangeIndex = 0; i_rangeIndex < l_memoryRanges.Count; ++i_rangeIndex)
            {
                if (l_memoryRanges[i_rangeIndex].start != m_memoryMapMemoryRanges[i_rangeIndex].start)
                {
                    return true;
                }
                if (l_memoryRanges[i_rangeIndex].end != m_memoryMapMemoryRanges[i_rangeIndex].end)
                {
                    return true;
                }
            }
            if (m_memoryMapSettings.GetDefaultMemoryValue() != m_memoryMapUninitializedMemoryValue)
            {
                return true;
            }

            if (m_peripheralsSettings.GetIOPanelEnabled() != m_peripheralsEnableIOPanel)
            {
                return true;
            }
            if (m_peripheralsSettings.GetTerminalEnabled() != m_peripheralsEnableTerminal)
            {
                return true;
            }
            if (m_peripheralsSettings.GetInterruptInjectorEnabled() != m_peripheralsEnableInterruptInjector)
            {
                return true;
            }

            if (m_terminalSettings.GetTextColorIndex() != m_terminalTextColorIndex)
            {
                return true;
            }
            if (m_terminalSettings.GetBackgroundColorIndex() != m_terminalBackgroundColorIndex)
            {
                return true;
            }

            if (m_logSettings.GetLoggingEnabled() != m_loggingEnable)
            {
                return true;
            }
            if (m_logSettings.GetVerbosityLevel() != m_loggingVerbosity)
            {
                return true;
            }
            if (m_logSettings.GetClearLogOnNewSimulation() != m_loggingClearOnNewSimulation)
            {
                return true;
            }
            if (m_logSettings.GetEcallLoggingDisable() != m_loggingDontLogEcall)
            {
                return true;
            }

            return false;
        }

        private bool CheckNewSettings()
        { // returns true if all settings are valid | for invalid settings shows a message and switches to the corresponding panel
            int? l_fontSize = m_editorSettings.GetFontSize();
            if (l_fontSize == null)
            {
                treeViewItemEditor.IsSelected = true;
                MessageBox.Show("Invalid font size.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (l_fontSize < 6 || l_fontSize > 48)
            {
                treeViewItemEditor.IsSelected = true;
                MessageBox.Show("Invalid font size.\nThe accepted range is 6..48", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            List<Interval> l_memoryRanges = m_memoryMapSettings.GetMemoryRanges();
            bool l_mainRangeFound = false;
            foreach (Interval i_range in l_memoryRanges)
            {
                if (i_range.start == 0x00000000 && i_range.end >= 0x80000)
                {
                    l_mainRangeFound = true;
                    break;
                }
            }
            if (l_mainRangeFound == false)
            {
                treeViewItemMemoryMap.IsSelected = true;
                MessageBox.Show("Memory range 0x00000000 .. 0x00080000 must be defined.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (m_emulatorSettings.GetUseIntegratedEcall())
            {
                UInt32? l_ecallBase = m_emulatorSettings.GetEcallBase();
                if (l_ecallBase == null)
                {
                    treeViewItemEmulator.IsSelected = true;
                    MessageBox.Show("Invalid ECALL base address.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if ((l_ecallBase & 0x3) != 0)
                {
                    treeViewItemEmulator.IsSelected = true;
                    MessageBox.Show("ECALL base address must be alligned to 32 bits.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (l_ecallBase < 0x10000000)
                {
                    treeViewItemEmulator.IsSelected = true;
                    MessageBox.Show("ECALL handler cannot be placed below 0x10000000.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                MemoryStream l_ecallHandlerStream = new MemoryStream(Properties.Resources.ecall_handler);
                UInt32 l_ecallEnd = (UInt32)l_ecallBase + (UInt32)l_ecallHandlerStream.Length - 1;
                bool l_ecallRangeFound = false;
                foreach (Interval i_range in l_memoryRanges)
                {
                    if (i_range.start <= l_ecallBase && i_range.end >= l_ecallEnd)
                    {
                        l_ecallRangeFound = true;
                        break;
                    }
                }
                if (l_ecallRangeFound == false)
                {
                    treeViewItemEmulator.IsSelected = true;
                    MessageBox.Show("ECALL handler must be placed in a defined memory space.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (m_peripheralsSettings.GetTerminalEnabled() == false)
                {
                    treeViewItemPeripherals.IsSelected = true;
                    MessageBox.Show("Terminal peripheral must be enabled if integrated ECALL handler is used.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (m_memoryMapSettings.GetDefaultMemoryValue() == null)
            {
                treeViewItemMemoryMap.IsSelected = true;
                MessageBox.Show("Invalid value for uninitialized memory.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (m_logSettings.GetLoggingEnabled())
            {
                if (m_logSettings.GetEcallLoggingDisable() == true && m_emulatorSettings.GetUseIntegratedEcall() == false)
                {
                    treeViewItemLogging.IsSelected = true;
                    MessageBox.Show("ECALL logging can be disabled only if the integrated ECALL handler is used.", "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void SaveSettings()
        { // this function considers that all settings are valid
            m_editorFontSize = (int)m_editorSettings.GetFontSize();
            m_editorEnableHighlighting = m_editorSettings.GetSyntaxHighlightingEnable();
            m_editorDefaultTemplate = m_editorSettings.GetNewFileTemplate();

            m_emulatorClearMemoryMap = m_emulatorSettings.GetClearMemoryMap();
            m_emulatorUseIntegratedEcallHandler = m_emulatorSettings.GetUseIntegratedEcall();
            UInt32? l_ecallBase = m_emulatorSettings.GetEcallBase();
            if (l_ecallBase == null)
            {
                m_emulatorSettings.SetEcallBase(m_emulatorEcallBase);
            }
            else
            {
                m_emulatorEcallBase = (UInt32)(l_ecallBase & ~0x3);
            }

            m_memoryMapMemoryRanges = m_memoryMapSettings.GetMemoryRanges();
            m_memoryMapUninitializedMemoryValue = (byte)m_memoryMapSettings.GetDefaultMemoryValue();

            m_peripheralsEnableIOPanel = m_peripheralsSettings.GetIOPanelEnabled();
            m_peripheralsEnableTerminal = m_peripheralsSettings.GetTerminalEnabled();
            m_peripheralsEnableInterruptInjector = m_peripheralsSettings.GetInterruptInjectorEnabled();

            m_terminalTextColorIndex = m_terminalSettings.GetTextColorIndex();
            m_terminalBackgroundColorIndex = m_terminalSettings.GetBackgroundColorIndex();

            m_loggingEnable = m_logSettings.GetLoggingEnabled();
            m_loggingVerbosity = m_logSettings.GetVerbosityLevel();
            m_loggingClearOnNewSimulation = m_logSettings.GetClearLogOnNewSimulation();
            m_loggingDontLogEcall = m_logSettings.GetEcallLoggingDisable();

            Properties.Settings.Default.editor_fontSize = m_editorFontSize;
            Properties.Settings.Default.editor_enableHighlighting = m_editorEnableHighlighting;
            Properties.Settings.Default.editor_defaultTemplate = (int)m_editorDefaultTemplate;

            Properties.Settings.Default.emulator_clearMemoryMap = m_emulatorClearMemoryMap;
            Properties.Settings.Default.emulator_useIntegratedEcallHandler = m_emulatorUseIntegratedEcallHandler;
            Properties.Settings.Default.emulator_ecallBase = m_emulatorEcallBase;

            UInt32[] l_memoryRanges = new UInt32[m_memoryMapMemoryRanges.Count << 1];
            for (int i_rangeIndex = 0; i_rangeIndex < l_memoryRanges.Length; i_rangeIndex += 2)
            {
                Interval l_range = m_memoryMapMemoryRanges[i_rangeIndex >> 1];

                l_memoryRanges[i_rangeIndex] = l_range.start;
                l_memoryRanges[i_rangeIndex + 1] = l_range.end;
            }
            Properties.Settings.Default.memoryMap_memoryRanges = l_memoryRanges;
            Properties.Settings.Default.memoryMap_uninitializedMemoryValue = m_memoryMapUninitializedMemoryValue;

            Properties.Settings.Default.peripherals_enableIOPanel = m_peripheralsEnableIOPanel;
            Properties.Settings.Default.peripherals_enableTerminal = m_peripheralsEnableTerminal;
            Properties.Settings.Default.peripherals_enableInterruptInjector = m_peripheralsEnableInterruptInjector;

            Properties.Settings.Default.terminal_textColorIndex = m_terminalTextColorIndex;
            Properties.Settings.Default.terminal_backgroundColorIndex = m_terminalBackgroundColorIndex;

            Properties.Settings.Default.logging_enable = m_loggingEnable;
            Properties.Settings.Default.logging_verbosity = (int)m_loggingVerbosity;
            Properties.Settings.Default.logging_clearOnNewSimulation = m_loggingClearOnNewSimulation;
            Properties.Settings.Default.logging_dontLogEcall = m_loggingDontLogEcall;

            Properties.Settings.Default.Save();
        }

        private void treeViewItemEditor_Selected(object sender, RoutedEventArgs e)
        {
            scrollViewerSettings.Content = m_editorSettings;
        }

        private void treeViewItemEmulator_Selected(object sender, RoutedEventArgs e)
        {
            scrollViewerSettings.Content = m_emulatorSettings;
        }

        private void treeViewItemMemoryMap_Selected(object sender, RoutedEventArgs e)
        {
            scrollViewerSettings.Content = m_memoryMapSettings;
            e.Handled = true; // prevent the event from triggering up the tree
        }

        private void treeViewItemPeripherals_Selected(object sender, RoutedEventArgs e)
        {
            scrollViewerSettings.Content = m_peripheralsSettings;
            e.Handled = true;
        }

        private void treeViewItemTerminal_Selected(object sender, RoutedEventArgs e)
        {
            scrollViewerSettings.Content = m_terminalSettings;
            e.Handled = true;
        }

        private void treeViewItemLogging_Selected(object sender, RoutedEventArgs e)
        {
            scrollViewerSettings.Content = m_logSettings;
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsChanged() == false)
            {
                return;
            }

            if (CheckNewSettings())
            {
                SaveSettings();
                MessageBox.Show("Settings saved.", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SettingsChanged())
            {
                MessageBoxResult l_result = MessageBox.Show("Settings changes were not saved.\nAre you sure you want to close this window?", "Settings unsaved", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (l_result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
