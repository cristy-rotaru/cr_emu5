using System;
using System.Collections.Generic;
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
    /// Interaction logic for EditorPerspective.xaml
    /// </summary>
    public partial class EditorPerspective : UserControl
    {
        public EditorPerspective()
        {
            InitializeComponent();
        }

        public bool CanUndo()
        {
            return textEditorMain.CanUndo;
        }

        public bool CanRedo()
        {
            return textEditorMain.CanRedo;
        }

        public void Undo()
        {
            textEditorMain.Undo();
        }

        public void Redo()
        {
            textEditorMain.Redo();
        }

        public void Cut()
        {
            textEditorMain.Cut();
        }

        public void Copy()
        {
            textEditorMain.Copy();
        }

        public void Paste()
        {
            textEditorMain.Paste();
        }
    }
}
