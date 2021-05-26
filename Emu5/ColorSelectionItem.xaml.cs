using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for ColorSelectionItem.xaml
    /// </summary>
    public partial class ColorSelectionItem : UserControl
    {
        public ColorSelectionItem(Brush color, String name)
        {
            InitializeComponent();

            rectangleColorPreview.Fill = color;
            textBlockColorName.Text = name;
        }
    }
}
