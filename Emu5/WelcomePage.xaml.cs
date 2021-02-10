using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : UserControl
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        public bool CloseOnNewTab()
        {
            return checkBoxCloseOnNewTab.IsChecked == true;
        }
    }
}
