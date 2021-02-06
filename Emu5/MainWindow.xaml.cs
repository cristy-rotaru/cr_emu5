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
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            
        }
        #endregion

        #region r_commandBindings
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

        #region r_menuItemsHandlers
        private void menuItemFileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
