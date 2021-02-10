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
            if (tabControlMain.Items.Count > 0)
            {
                TabItem l_firstTab = (TabItem)tabControlMain.Items[0];
                
                if (l_firstTab.Content.GetType() == typeof(WelcomePage))
                {
                    WelcomePage l_welcomePage = (WelcomePage)l_firstTab.Content;
                    
                    if (l_welcomePage.CloseOnNewTab())
                    {
                        tabControlMain.Items.Remove(l_firstTab);
                    }
                }
            }

            TabItem l_newTab = new TabItem();
            l_newTab.Header = new TabHeader("Untitled", true, () => { RemoveTab(l_newTab); });
            l_newTab.Content = new PerspectivePage();
            tabControlMain.Items.Add(l_newTab);

            tabControlMain.SelectedItem = l_newTab;
        }

        void commandOpen_Executed(object target, ExecutedRoutedEventArgs e)
        {

        }

        void commandSave_Executed(object target, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Save");
        }

        void commandSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        if (l_page.GetCurrentPerspective() == Perspective.Editor || l_page.GetCurrentPerspective() == Perspective.Log)
                        {
                            l_canExecute = true;
                        }
                    }
                }
            }

            e.CanExecute = l_canExecute;
        }

        void commandSaveAs_Executed(object target, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Save As");
        }

        void commandSaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        if (l_page.GetCurrentPerspective() == Perspective.Editor || l_page.GetCurrentPerspective() == Perspective.Log)
                        {
                            l_canExecute = true;
                        }
                    }
                }
            }

            e.CanExecute = l_canExecute;
        }

        void commandUndo_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.Undo();
        }

        void commandUndo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        l_canExecute = l_page.CanUndo();
                    }
                }
            }

            e.CanExecute = l_canExecute;
        }

        void commandRedo_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.Redo();
        }

        void commandRedo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        l_canExecute = l_page.CanRedo();
                    }
                }
            }

            e.CanExecute = l_canExecute;
        }

        void commandCut_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.Cut();
        }

        void commandCut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        if (l_page.GetCurrentPerspective() == Perspective.Editor || l_page.GetCurrentPerspective() == Perspective.Log)
                        {
                            l_canExecute = true;
                        }
                    }
                }
            }

            e.CanExecute = l_canExecute;
        }

        void commandCopy_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.Copy();
        }

        void commandCopy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        if (l_page.GetCurrentPerspective() == Perspective.Editor || l_page.GetCurrentPerspective() == Perspective.Log)
                        {
                            l_canExecute = true;
                        }
                    }
                }
            }

            e.CanExecute = l_canExecute;
        }

        void commandPaste_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.Paste();
        }

        void commandPaste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        PerspectivePage l_page = (PerspectivePage)l_tab.Content;

                        if (l_page.GetCurrentPerspective() == Perspective.Editor)
                        {
                            l_canExecute = true;
                        }
                    }
                }
            }

            e.CanExecute = l_canExecute;
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

        void commandOpenIOPanel_Executed(object target, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Open I/O Panel");
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
            else if (tab.Content.GetType() == typeof(PerspectivePage))
            {
                // TODO: perform check if file is saved
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
