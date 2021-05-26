using System.Collections.Generic;
using System.Windows;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        EditorSettingsPanel m_editorSettings;
        EmulatorSettingsPanel m_emulatorSettings;
        MemoryMapSettingsPanel m_memoryMapSettings;
        PeripheralsSettingsPanel m_peripheralsSettings;
        TerminalSettingsPanel m_terminalSettings;
        LogSettingsPanel m_logSettings;

        public SettingsWindow()
        {
            InitializeComponent();

            m_editorSettings = new EditorSettingsPanel();
            m_emulatorSettings = new EmulatorSettingsPanel();
            m_memoryMapSettings = new MemoryMapSettingsPanel();
            m_peripheralsSettings = new PeripheralsSettingsPanel();
            m_terminalSettings = new TerminalSettingsPanel();
            m_logSettings = new LogSettingsPanel();

            LoadSettings();
            treeViewItemEditor.IsSelected = true;
        }

        public void LoadSettings()
        {
            m_editorSettings.SetFontSize(15);
            m_editorSettings.SetSyntaxHighlighting(true);
            m_editorSettings.SetNewFileTemplate(ProgramTemplate.Basic);

            m_emulatorSettings.SetClearMemoryMap(false);
            m_emulatorSettings.SetUseIntegratedEcall(true);
            m_emulatorSettings.SetEcallBase(0xFFFFF000);

            List<Interval> l_memoryRanges = new List<Interval>();
            l_memoryRanges.Add(new Interval { start = 0x00000000, end = 0x9FFFFFFF });
            l_memoryRanges.Add(new Interval { start = 0xC0000000, end = 0xFFFFFFFF });
            m_memoryMapSettings.SetMemoryRanges(l_memoryRanges);
            m_memoryMapSettings.SetDefaultMemoryValue(0xFF);

            m_peripheralsSettings.SetIOPanelEnabled(true);
            m_peripheralsSettings.SetTerminalEnabled(true);
            m_peripheralsSettings.SetInterruptInjectorEnabled(false);

            m_terminalSettings.SetTextColorIndex(21);
            m_terminalSettings.SetBackgroundColorIndex(0);

            m_logSettings.SetLoggingEnabled(true);
            m_logSettings.SetVerbosityLevel(Verbosity.Normal);
            m_logSettings.SetEcallLoggingDisable(true);
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
