using System;
using System.Windows;
using System.Windows.Controls;

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

        TabHeader m_tabHeader = null;

        EditorPerspective m_editor = new EditorPerspective();

        RVEmulator m_rvEmulator = null;

        public PerspectivePage()
        {
            InitializeComponent();

            dockPanelMain.Children.Add(m_editor);
        }

        public PerspectivePage(TabHeader tabHeader) : this()
        {
            m_tabHeader = tabHeader;
            m_editor.RegisterFileModifiedCallback(() => m_tabHeader.SetSavedState(true));
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

        public bool Save()
        {
            bool l_result = false;

            if (m_currentPerspective == Perspective.Editor)
            {
                l_result = m_editor.Save();

                if (l_result)
                {
                    m_tabHeader?.ChangeHeaderText(m_editor.GetFileName(), false);
                }
            }
            else if (m_currentPerspective == Perspective.Log)
            {
                // save log
            }

            return l_result;
        }

        public void SaveAs()
        {
            if (m_currentPerspective == Perspective.Editor)
            {
                if (m_editor.SaveAs())
                {
                    m_tabHeader?.ChangeHeaderText(m_editor.GetFileName(), false);
                }
            }
            else if (m_currentPerspective == Perspective.Log)
            {
                // save log
            }
        }

        public bool Open()
        {
            bool l_result = m_editor.Open();

            if (l_result == true)
            {
                m_tabHeader?.ChangeHeaderText(m_editor.GetFileName(), false);
            }

            return l_result;
        }

        public String GetFileName()
        {
            return m_editor.GetFileName();
        }

        public void StartEmulator()
        {
            if (m_rvEmulator == null)
            {
                m_rvEmulator = new RVEmulator();
            }

            try
            {
                m_rvEmulator.Assemble(m_editor.GetText());
            }
            catch (RVAssemblyException e_assemblyException)
            {
                MessageBox.Show("L: " + e_assemblyException.Line + "; C: " + e_assemblyException.Column + "\n" + e_assemblyException.Message, "Compilation error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
