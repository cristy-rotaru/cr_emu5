using System;
using System.Threading;
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
        public delegate void PerspectiveChangedDelegate();

        Perspective m_currentPerspective = Perspective.Editor;
        PerspectiveChangedDelegate m_perspectiveChangedCallback = null;

        TabHeader m_tabHeader = null;

        EditorPerspective m_editor = new EditorPerspective();
        EmulatorPerspective m_processor = new EmulatorPerspective();

        RVEmulator m_rvEmulator = new RVEmulator();

        bool m_simulationRunning, m_compiling;

        public bool IsRunning
        {
            get
            {
                return m_simulationRunning;
            }
        }

        public PerspectivePage()
        {
            InitializeComponent();

            dockPanelMain.Children.Add(m_editor);

            m_simulationRunning = false;
            m_compiling = false;
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

        public void RegisterPerspectiveChangedCallback(PerspectiveChangedDelegate handler)
        {
            m_perspectiveChangedCallback = handler;
        }

        public void ChangePerspective(Perspective newPerspective)
        {
            if (newPerspective != Perspective.None)
            {
                m_currentPerspective = newPerspective;
                dockPanelMain.Children.Clear();

                switch(m_currentPerspective)
                {
                    case Perspective.Editor:
                    {
                        dockPanelMain.Children.Add(m_editor);
                    }
                    break;

                    case Perspective.Emulator:
                    {
                        dockPanelMain.Children.Add(m_processor);
                    }
                    break;

                    case Perspective.Log:
                    {
                        // will add log perspective once implemented
                    }
                    break;
                }

                m_perspectiveChangedCallback?.Invoke();
            }
        }

        public bool CanUndo()
        {
            return m_currentPerspective == Perspective.Editor ? m_editor.CanUndo() : false;
        }

        public bool CanRedo()
        {
            return m_currentPerspective == Perspective.Editor ? m_editor.CanRedo() : false;
        }

        public bool CanStartEmulator()
        {
            return !m_compiling;
        }

        public bool CanStep()
        {
            return m_simulationRunning;
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
            m_simulationRunning = true;
            m_compiling = true;

            String l_code = m_editor.GetText();
            m_processor.BindEmulator(m_rvEmulator);
            m_editor.SetEditable(false);

            ThreadStart l_startEmulatorThreadFunction = new ThreadStart(
            () => {
                try
                {
                    m_rvEmulator.Assemble(l_code);
                    m_rvEmulator.ResetProcessor();

                    Delegate l_compilationFinishedDelegate = new Action(
                    () => {
                        m_editor.SetEditable(true);
                        m_processor.UpdateInfo();

                        m_compiling = false;

                        ChangePerspective(Perspective.Emulator);
                    });

                    Dispatcher.BeginInvoke(l_compilationFinishedDelegate);
                }
                catch (RVAssemblyException e_assemblyException)
                {
                    Delegate l_exceptionDelegate = new Action(
                    () => {
                        ChangePerspective(Perspective.Editor);

                        MessageBox.Show("L: " + e_assemblyException.Line + "; C: " + e_assemblyException.Column + "\n" + e_assemblyException.Message, "Compilation error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        m_editor.SetEditable(true);

                        m_compiling = false;
                        m_simulationRunning = false;
                    });

                    Dispatcher.BeginInvoke(l_exceptionDelegate);
                }
            });

            Thread l_worker = new Thread(l_startEmulatorThreadFunction);
            l_worker.Start();
        }

        public void Step()
        {

        }
    }
}
