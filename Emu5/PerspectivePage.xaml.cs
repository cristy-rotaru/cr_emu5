﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        IOPanel m_IOPeripheral;
        InterruptInjector m_interruptInjectorPeripheral;

        System.Timers.Timer m_clockTimer;
        Thread m_simulationThread;
        object m_threadSync;

        bool m_simulationRunning, m_compiling;
        bool m_runningClocked, m_runningFast;

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

            m_clockTimer = new System.Timers.Timer(110);
            m_clockTimer.Elapsed += ClockTick;

            m_threadSync = new object();

            m_simulationRunning = false;
            m_compiling = false;
            m_runningClocked = false;
            m_runningFast = false;

            m_IOPeripheral = new IOPanel(m_rvEmulator);
            m_interruptInjectorPeripheral = new InterruptInjector(m_rvEmulator);
            m_rvEmulator.GetMemoryMapReference().RegisterPeripheral(m_IOPeripheral, 0x0110, 8);
            m_rvEmulator.GetMemoryMapReference().RegisterPeripheral(m_interruptInjectorPeripheral, 0x0118, 4);
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

        public bool CanRun()
        {
            return m_simulationRunning && !m_compiling && !m_runningClocked && !m_runningFast;
        }

        public bool CanPause()
        {
            return m_simulationRunning && (m_runningClocked || m_runningFast);
        }

        public bool CanStopSimulation()
        {
            return m_simulationRunning && !m_compiling;
        }

        public bool CanInjectInterrupt()
        {
            return m_simulationRunning && !m_compiling;
        }

        public bool CanOpenPeripheralUI()
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
            bool l_wasRunningClocked = m_runningClocked;
            bool l_wasRunningFast = m_runningFast;

            if (m_runningClocked)
            {
                m_clockTimer.Stop();
                m_runningClocked = false;
            }
            else if (m_runningFast)
            {
                lock (m_threadSync)
                {
                    m_runningFast = false;
                }
                m_simulationThread.Join();
                m_simulationThread = null;
            }

            if (m_simulationRunning)
            {
                String l_message = "The simulation is running.\nDo you want to stop it and launch a new one?";
                MessageBoxResult l_result = MessageBox.Show(l_message, "Simulation already running", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (l_result != MessageBoxResult.Yes)
                { // resume where we left off
                    if (l_wasRunningClocked)
                    {
                        m_runningClocked = true;
                        m_clockTimer.Start();
                    }
                    else if (l_wasRunningFast)
                    {
                        m_runningFast = true;

                        ThreadStart l_runThreadFunction = new ThreadStart(RunFastSimulation);

                        m_simulationThread = new Thread(l_runThreadFunction);
                        m_simulationThread.Start();
                    }

                    return;
                }
            }

            m_simulationRunning = true;
            m_compiling = true;

            String l_code = m_editor.GetText();
            m_processor.BindEmulator(m_rvEmulator);
            m_editor.SetEditable(false);

            ThreadStart l_startEmulatorThreadFunction = new ThreadStart(
            () => {
                try
                {
                    RVLabelReferenceMap l_labelMap = new RVLabelReferenceMap();
                    Dictionary<UInt32, String> l_pseudoInstructionMap = new Dictionary<UInt32, String>();

                    m_rvEmulator.Assemble(l_code, l_labelMap, l_pseudoInstructionMap);
                    m_rvEmulator.ResetProcessor();

                    Delegate l_compilationFinishedDelegate = new Action(
                    () => {
                        m_editor.SetEditable(true);
                        m_processor.SetLabelReferences(l_labelMap);
                        m_processor.SetPseudoInstructionReference(l_pseudoInstructionMap);
                        m_processor.UpdateInfo();
                        m_processor.HighlightingEnabled = true;

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
            m_rvEmulator.SingleStep();

            m_processor.UpdateInfo();

            if (m_rvEmulator.Halted)
            {
                m_simulationRunning = false;

                MessageBox.Show("Simulation stopped.\nCore halted: " + m_rvEmulator.HaltReason, "Core halted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void RunClocked()
        {
            m_runningClocked = true;
            m_clockTimer.Start();
        }

        public void Run()
        {
            m_runningFast = true;

            m_processor.HighlightingEnabled = false;

            ThreadStart l_runThreadFunction = new ThreadStart(RunFastSimulation);

            m_simulationThread = new Thread(l_runThreadFunction);
            m_simulationThread.Start();
        }

        public void Pause()
        {
            if (m_runningClocked)
            {
                m_clockTimer.Stop();
                m_runningClocked = false;
            }
            else if (m_runningFast)
            {
                lock (m_threadSync)
                {
                    m_runningFast = false;
                }
                m_simulationThread.Join();
                m_simulationThread = null;

                m_processor.UpdateInfo();
                m_processor.HighlightingEnabled = true;
            }
        }

        public void StopSimulation()
        {
            if (m_runningClocked)
            {
                m_clockTimer.Stop();
                m_runningClocked = false;
            }
            else if (m_runningFast)
            {
                lock (m_threadSync)
                {
                    m_runningFast = false;
                }
                m_simulationThread.Join();
                m_simulationThread = null;

                m_processor.UpdateInfo();
                m_processor.HighlightingEnabled = true;
            }
            
            m_simulationRunning = false;

            m_rvEmulator.Halted = true;
        }

        public void OpenInjectInterruptUI()
        {
            InjectInterruptWindow l_injectInterruptUI = new InjectInterruptWindow(GetFileName(), m_rvEmulator);
            l_injectInterruptUI.ShowDialog();
        }

        public void OpenIOPanelPeripheralUI()
        {
            IOPanelWindow l_windowHandle = new IOPanelWindow(m_IOPeripheral);
            l_windowHandle.Title = "I/O panel for " + GetFileName();
            l_windowHandle.Show();
        }

        private void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                m_rvEmulator.SingleStep();

                m_processor.UpdateInfo();

                if (m_rvEmulator.Halted)
                {
                    m_clockTimer.Stop();

                    m_simulationRunning = false;
                    m_runningClocked = false;

                    MessageBox.Show("Simulation stopped.\nCore halted: " + m_rvEmulator.HaltReason, "Core halted", MessageBoxButton.OK, MessageBoxImage.Information);

                    CommandManager.InvalidateRequerySuggested();
                }

                if (m_rvEmulator.BreakpointHit())
                {
                    m_clockTimer.Stop();
                    m_runningClocked = false;

                    CommandManager.InvalidateRequerySuggested();
                }
            }));
        }

        private void RunFastSimulation()
        {
            for (;;) // simulate in a infinite loop
            {
                bool l_shutdown;

                lock (m_threadSync)
                {
                    l_shutdown = !m_runningFast;
                }

                if (l_shutdown)
                {
                    return; // stop simulation per external request
                }

                m_rvEmulator.SingleStep();

                if (m_rvEmulator.Halted)
                {
                    Delegate l_simulationEndedDelegate = new Action(
                    () => {
                        m_simulationRunning = false;
                        m_runningFast = false;

                        m_processor.UpdateInfo();
                        m_processor.HighlightingEnabled = true;

                        MessageBox.Show("Simulation stopped.\nCore halted: " + m_rvEmulator.HaltReason, "Core halted", MessageBoxButton.OK, MessageBoxImage.Information);

                        CommandManager.InvalidateRequerySuggested();
                    });

                    Dispatcher.BeginInvoke(l_simulationEndedDelegate);

                    return;
                }

                if (m_rvEmulator.BreakpointHit())
                {
                    Delegate l_breakpointHitDelegate = new Action(
                    () => {
                        m_runningFast = false;

                        m_processor.UpdateInfo();
                        m_processor.HighlightingEnabled = true;

                        CommandManager.InvalidateRequerySuggested();
                    });

                    Dispatcher.BeginInvoke(l_breakpointHitDelegate);

                    return;
                }
            }
        }
    }
}
