using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region r_windowEvents
        private void MainWindow_Initialized(object sender, EventArgs e)
        {
            TabItem l_welcomeTab = new TabItem();
            l_welcomeTab.Header = new TabHeader("Welcome", false, () => { RemoveTab(l_welcomeTab); });
            l_welcomeTab.Content = new WelcomePage();
            tabControlMain.Items.Add(l_welcomeTab);

            tabControlMain.SelectedIndex = 0;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            
        }
        #endregion

        #region r_commandBindings
        #region r_applicationCommands
        void commandNew_Executed(object target, ExecutedRoutedEventArgs e)
        {
            
        }

        void commandOpen_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandSave_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandSaveAs_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandSaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandUndo_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandUndo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandRedo_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandRedo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandCut_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandCut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandCopy_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandCopy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandPaste_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandPaste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }
        #endregion

        #region r_emulatorCommands
        void commandStartEmulator_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandStartEmulator_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandStep_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandStep_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandRunClocked_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandRunClocked_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandRun_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandRun_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandPause_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandPause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandInjectInterrupt_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandInjectInterrupt_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandStop_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandStop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        void commandOpenTerminal_Executed(object target, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Open Terminal");
        }

        void commandOpenTerminal_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void commandOpenIOPanel_Executed(object target, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Open I/O Panel");
        }

        void commandOpenIOPanel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        #endregion
        #endregion

        #region r_menuItemsHandlers
        private void menuItemFileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuItemHelpSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuItemHelpAbout_Click(object sender, RoutedEventArgs e)
        {

        }
        #endregion

        #region r_tabControlHandling
        private void RemoveTab(TabItem tab)
        {
            if (tab.Content.GetType() == typeof(WelcomePage))
            {
                tabControlMain.Items.Remove(tab);
            }

            if (tabControlMain.Items.Count == 0) // add welcome tab if no other tabs are available
            {
                TabItem l_welcomeTab = new TabItem();
                l_welcomeTab.Header = new TabHeader("Welcome", false, () => { RemoveTab(l_welcomeTab); });
                l_welcomeTab.Content = new WelcomePage();
                tabControlMain.Items.Add(l_welcomeTab);

                tabControlMain.SelectedIndex = 0;
            }
        }

        private void tabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        #endregion
    }
}
