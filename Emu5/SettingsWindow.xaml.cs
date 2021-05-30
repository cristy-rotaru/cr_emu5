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

        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
