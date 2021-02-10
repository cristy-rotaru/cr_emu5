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
    public enum Perspective
    {
        None = 0,
        Editor,
        Emulator,
        Log
    }
    /// <summary>
    /// Interaction logic for PerspectivePage.xaml
    /// </summary>
    public partial class PerspectivePage : UserControl
    {
        Perspective m_currentPerspective = Perspective.Editor;

        EditorPerspective m_editor = new EditorPerspective();

        public PerspectivePage()
        {
            InitializeComponent();

            dockPanelMain.Children.Add(m_editor);
        }

        public Perspective GetCurrentPerspective()
        {
            return m_currentPerspective;
        }

        public bool CanUndo()
        {
            return m_currentPerspective == Perspective.Editor ? m_editor.CanUndo() : false;
        }

        public bool CanRedo()
        {
            return m_currentPerspective == Perspective.Editor ? m_editor.CanRedo() : false;
        }

        public void Undo()
        {
            if (m_currentPerspective == Perspective.Editor)
            {
                m_editor.Undo();
            }
        }

        public void Redo()
        {
            if (m_currentPerspective == Perspective.Editor)
            {
                m_editor.Redo();
            }
        }

        public void Cut()
        {
            if (m_currentPerspective == Perspective.Editor)
            {
                m_editor.Cut();
            }
            else if (m_currentPerspective == Perspective.Log)
            {
                // cut from log
            }
        }

        public void Copy()
        {
            if (m_currentPerspective == Perspective.Editor)
            {
                m_editor.Copy();
            }
            else if (m_currentPerspective == Perspective.Log)
            {
                // copy from log
            }
        }

        public void Paste()
        {
            if (m_currentPerspective == Perspective.Editor)
            {
                m_editor.Paste();
            }
        }
    }
}
