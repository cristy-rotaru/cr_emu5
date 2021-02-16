using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("En-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("En-US");

            TabItem l_welcomeTab = new TabItem();
            l_welcomeTab.Header = new TabHeader("Welcome", false, () => RemoveTab(l_welcomeTab));
            l_welcomeTab.Content = new WelcomePage();
            tabControlMain.Items.Add(l_welcomeTab);

            tabControlMain.SelectedIndex = 0;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            foreach (TabItem i_tab in tabControlMain.Items)
            {
                if (i_tab.Content.GetType() == typeof(PerspectivePage))
                {
                    if (((TabHeader)i_tab.Header).IsUnsaved())
                    {
                        String l_fileName = ((PerspectivePage)i_tab.Content).GetFileName();
                        String l_message = l_fileName == null ? "File was not saved." : "Latest changes to " + l_fileName + " were not saved";
                        l_message += "\nDo you want to save before closing?";

                        tabControlMain.SelectedItem = i_tab;
                        tabControlMain.UpdateLayout();

                        MessageBoxResult l_result =  MessageBox.Show(l_message, "File not saved", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        if (l_result == MessageBoxResult.Yes)
                        {
                            if (((PerspectivePage)i_tab.Content).Save() == false)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                        else if (l_result == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                }
            }
        }
        #endregion

        #region r_commandBindings
        #region r_applicationCommands
        void commandNew_Executed(object target, ExecutedRoutedEventArgs e)
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

            TabItem l_newTab = new TabItem();
            l_newTab.Header = new TabHeader("Untitled", true, () => RemoveTab(l_newTab));
            l_newTab.Content = new PerspectivePage((TabHeader)l_newTab.Header);
            tabControlMain.Items.Add(l_newTab);

            tabControlMain.SelectedItem = l_newTab;
        }

        void commandOpen_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_newTab = new TabItem();
            l_newTab.Header = new TabHeader("", false, () => RemoveTab(l_newTab));
            l_newTab.Content = new PerspectivePage((TabHeader)l_newTab.Header);

            if (((PerspectivePage)l_newTab.Content).Open() == true)
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

                tabControlMain.Items.Add(l_newTab);

                tabControlMain.SelectedItem = l_newTab;
            }
        }

        void commandSave_Executed(object target, ExecutedRoutedEventArgs e)
        {
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.Save();
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
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.SaveAs();
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
            TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];
            PerspectivePage l_page = (PerspectivePage)l_tab.Content;
            l_page.StartEmulator();
        }

        void commandStartEmulator_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            bool l_canExecute = false;

            if (tabControlMain != null)
            {
                if (tabControlMain.SelectedIndex >= 0)
                {
                    TabItem l_tab = (TabItem)tabControlMain.Items[tabControlMain.SelectedIndex];

                    if (l_tab.Content.GetType() == typeof(PerspectivePage))
                    {
                        l_canExecute = true;
                    }
                }
            }

            e.CanExecute = l_canExecute;
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
                TabHeader l_header = (TabHeader)tab.Header;

                if (l_header.IsUnsaved() == false)
                {
                    tabControlMain.Items.Remove(tab);
                }
                else if (MessageBox.Show("The file is not saved.\nAll progress will be lost.\nAre you sure you want to close this tab?", "File not saved", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    tabControlMain.Items.Remove(tab);
                }
            }

            if (tabControlMain.Items.Count == 0) // add welcome tab if no other tabs are available
            {
                TabItem l_welcomeTab = new TabItem();
                l_welcomeTab.Header = new TabHeader("Welcome", false, () => RemoveTab(l_welcomeTab));
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
