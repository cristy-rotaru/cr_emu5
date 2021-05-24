using System.Windows;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        EditorSettingsPanel m_editorSettings;

        public SettingsWindow()
        {
            InitializeComponent();

            m_editorSettings = new EditorSettingsPanel();

            LoadSettings();
            treeViewItemEditor.IsSelected = true;
        }

        public void LoadSettings()
        {
            m_editorSettings.SetFontSize(15);
            m_editorSettings.SetSyntaxHighlighting(true);
            m_editorSettings.SetNewFileTemplate(ProgramTemplate.Basic);
        }

        private void treeViewItemEditor_Selected(object sender, RoutedEventArgs e)
        {
            contentControlSettings.Content = m_editorSettings;
        }

        private void treeViewItemEmulator_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void treeViewItemMemoryMap_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void treeViewItemPeripherals_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void treeViewItemTerminal_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void treeViewItemLogging_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
