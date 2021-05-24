using System.Windows;
using System.Windows.Controls;

namespace Emu5
{
    public enum ProgramTemplate
    {
        Empty = 0,
        Basic,
        Advanced
    }
    /// <summary>
    /// Interaction logic for EditorSettingsPanel.xaml
    /// </summary>
    public partial class EditorSettingsPanel : UserControl
    {
        public EditorSettingsPanel()
        {
            InitializeComponent();
        }

        public void SetFontSize(int size)
        {
            textBoxFontSize.Text = size.ToString();
        }

        public void SetSyntaxHighlighting(bool enabled)
        {
            checkBoxSyntaxHighlighting.IsChecked = enabled;
        }

        public void SetNewFileTemplate(ProgramTemplate template)
        {
            comboBoxFileTemplate.SelectedIndex = (int)template;
        }

        public int? GetFontSize()
        {
            try
            {
                int l_size = int.Parse(textBoxFontSize.Text);
                return l_size;
            }
            catch
            {
                return null;
            }
        }

        public bool GetSyntaxHighlightingEnable()
        {
            return checkBoxSyntaxHighlighting.IsChecked == null ? false : (bool)checkBoxSyntaxHighlighting.IsChecked;
        }

        public ProgramTemplate GetNewFileTemplate()
        {
            return (ProgramTemplate)comboBoxFileTemplate.SelectedIndex;
        }

        private void textBoxFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int l_size = int.Parse(textBoxFontSize.Text);

                if (l_size < 6)
                {
                    textBlockInvalidFontSize.Text = "(Value too small)";
                    textBlockInvalidFontSize.Visibility = Visibility.Visible;
                }
                else if (l_size > 48)
                {
                    textBlockInvalidFontSize.Text = "(Value too large)";
                    textBlockInvalidFontSize.Visibility = Visibility.Visible;
                }
                else
                {
                    textBlockInvalidFontSize.Visibility = Visibility.Hidden;
                }
            }
            catch
            {
                textBlockInvalidFontSize.Text = "(Invalid number)";
                textBlockInvalidFontSize.Visibility = Visibility.Visible;
            }
        }

        private void comboBoxFileTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textBlockTemplateDescription == null)
            {
                return;
            }

            ProgramTemplate l_template = (ProgramTemplate)comboBoxFileTemplate.SelectedIndex;

            switch (l_template)
            {
                case ProgramTemplate.Empty:
                {
                    textBlockTemplateDescription.Text = "When a new file is created it will be empty.";
                }
                break;

                case ProgramTemplate.Basic:
                {
                    textBlockTemplateDescription.Text = "This template will create a simple program body.";
                }
                break;

                case ProgramTemplate.Advanced:
                {
                    textBlockTemplateDescription.Text = "This template will create a simple program body and will define all the traps and interrupts.";
                }
                break;
            }
        }
    }
}
