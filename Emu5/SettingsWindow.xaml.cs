using System;
using System.Windows;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            treeViewItemEditor.IsSelected = true;
        }

        private void treeViewItemEditor_Selected(object sender, RoutedEventArgs e)
        {
            
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
    }
}
