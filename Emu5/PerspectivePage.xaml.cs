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

        EditorPerspective m_editor = null;
        EmulatorPerspective m_processor = null;
        LogPerspective m_logger = null;

        RVEmulator m_rvEmulator = null;

        IOPanel m_IOPeripheral;
        InterruptInjector m_interruptInjectorPeripheral;
        Terminal m_terminalPeripheral;

        IOPanelWindow m_IOPanelWindowHandle;
        TerminalWindow m_terminalWindowHandle;

        System.Timers.Timer m_clockTimer;
        Thread m_simulationThread;
        object m_threadSync;

        bool m_simulationRunning, m_compiling, m_wasCompiled;
        bool m_runningClocked, m_runningFast;
        bool m_simulationJustStarted, m_simulationJustPaused, m_breakpointHit;

        int m_stepCount;

        public bool IsRunning
        {
            get
            {
                return m_simulationRunning;
            }
        }

        public bool JustStarted
        {
            get
            {
                return m_simulationJustStarted;
            }
        }

        public bool JustPaused
        {
            get
            {
                bool l_toReturn = m_simulationJustPaused;
                m_simulationJustPaused = false;
                return l_toReturn;
            }
        }

        public bool RunningFastSimulation
        {
            get
            {
                return m_runningFast;
            }
        }

        public bool BreakpointHit
        {
            get
            {
                bool l_toReturn = m_breakpointHit;
                m_breakpointHit = false;
                return l_toReturn;
            }
        }

        public PerspectivePage()
        {
            InitializeComponent();

            m_editor = new EditorPerspective();
            m_processor = new EmulatorPerspective(this);
            m_logger = new LogPerspective();

            m_rvEmulator = new RVEmulator();

            dockPanelMain.Children.Add(m_editor);

            m_clockTimer = new System.Timers.Timer(110);
            m_clockTimer.Elapsed += ClockTick;

            m_threadSync = new object();

            m_simulationRunning = false;
            m_compiling = false;
            m_wasCompiled = false;
            m_runningClocked = false;
            m_runningFast = false;
            m_simulationJustStarted = false;
            m_simulationJustPaused = false;
            m_breakpointHit = false;

            m_IOPeripheral = new IOPanel(m_rvEmulator, this);
            m_interruptInjectorPeripheral = new InterruptInjector(m_rvEmulator);
            m_terminalPeripheral = new Terminal(m_rvEmulator, this);

            m_IOPanelWindowHandle = null;
            m_terminalWindowHandle = null;

            ApplySettings();
        }

        public PerspectivePage(TabHeader tabHeader) : this()
        {
            m_tabHeader = tabHeader;
            m_editor.RegisterFileModifiedCallback(() => m_tabHeader.SetSavedState(true));
        }

        public void ApplySettings()
        {
            CloseAllPeripheralWindows();

            m_editor.SetFontSize(Properties.Settings.Default.editor_fontSize);
            m_editor.SetSyntaxHighlightingEnabled(Properties.Settings.Default.editor_enableHighlighting);

            RVMemoryMap l_memoryMap = m_rvEmulator.GetMemoryMapReference();
            UInt32[] l_rangeSettingArray = Properties.Settings.Default.memoryMap_memoryRanges; // even positions contain start of range | odd positions contain end of range
            l_memoryMap.ResetMemoryRanges();
            if ((l_rangeSettingArray.Length & 1) == 0) // array must have an even number of entries
            {
                for (int i_rangeStart = 0; i_rangeStart < l_rangeSettingArray.Length; i_rangeStart += 2)
                {
                    Interval l_memoryRange = new Interval { start = l_rangeSettingArray[i_rangeStart], end = l_rangeSettingArray[i_rangeStart + 1] };
                    l_memoryMap.AddMemoryRange(l_memoryRange);
                }
            }
            l_memoryMap.UninitializedMemoryValue = Properties.Settings.Default.memoryMap_uninitializedMemoryValue;

            l_memoryMap.UnregisterAllPeripherals();
            if (Properties.Settings.Default.peripherals_enableIOPanel)
            {
                l_memoryMap.RegisterPeripheral(m_IOPeripheral, 0x0110, 8);
            }
            if (Properties.Settings.Default.peripherals_enableTerminal)
            {
                l_memoryMap.RegisterPeripheral(m_terminalPeripheral, 0x011C, 4);
            }
            if (Properties.Settings.Default.peripherals_enableInterruptInjector)
            {
                l_memoryMap.RegisterPeripheral(m_interruptInjectorPeripheral, 0x0118, 4);
            }

            if (Properties.Settings.Default.logging_enable)
            {
                m_rvEmulator.RegisterLogger(m_logger);
                m_rvEmulator.SetLoggingVerbosity((Verbosity)Properties.Settings.Default.logging_verbosity);
                m_rvEmulator.DisableEcallLogging = Properties.Settings.Default.logging_dontLogEcall;
            }
            else
            {
                m_rvEmulator.RegisterLogger(null);
                m_logger.Clear();
            }
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
                        dockPanelMain.Children.Add(m_logger);
                    }
                    break;
                }

                m_perspectiveChangedCallback?.Invoke();
            }
        }

        public bool CanSaveMemory()
        {
            return m_wasCompiled && !m_compiling && !m_runningClocked && !m_runningFast;
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

        public bool CanOpenIOPanelPeripheralUI()
        {
            return m_simulationRunning & Properties.Settings.Default.peripherals_enableIOPanel;
        }

        public bool CanOpenTerminalPeripheralUI()
        {
            return m_simulationRunning & Properties.Settings.Default.peripherals_enableTerminal;
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
                m_logger.Save();
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
                m_logger.Save();
            }
        }

        public void SaveMemory()
        {
            (new SaveMemoryWindow(m_rvEmulator.GetMemoryMapReference(), GetFileName())).ShowDialog();
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

        public void LoadTemplate()
        {
            ProgramTemplate l_template = (ProgramTemplate)Properties.Settings.Default.editor_defaultTemplate;
            bool l_useIntegratedEcallHandler = Properties.Settings.Default.emulator_useIntegratedEcallHandler;
            UInt32 l_ecallBase = Properties.Settings.Default.emulator_ecallBase;

            m_editor.GenerateTemplate(l_template, 0x20000, 0x7FFFC, l_useIntegratedEcallHandler, l_ecallBase, 0x300);
        }

        public void NotifyUpdateRequired()
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                if (m_simulationRunning && !m_runningFast)
                {
                    m_processor.UpdateInfo();
                }
            }));
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

                if (Properties.Settings.Default.logging_enable)
                {
                    m_logger.LogText("Simulation ended:", false);
                    m_logger.LogCurrentTime(false);
                    m_logger.LogText("(Stopped by user)", true);
                    m_logger.NewLine();
                    m_logger.NewLine();
                    m_logger.UpdateLogUI();
                }
            }

            m_stepCount = 0;

            if (Properties.Settings.Default.logging_clearOnNewSimulation)
            {
                m_logger.Clear();
            }

            if (Properties.Settings.Default.logging_enable)
            {
                m_logger.LogText("Simulation started:", false);
                m_logger.LogCurrentTime(true);
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
                    if (Properties.Settings.Default.emulator_clearMemoryMap)
                    {
                        m_rvEmulator.GetMemoryMapReference().Clear();
                    }

                    RVLabelReferenceMap l_labelMap = new RVLabelReferenceMap();
                    Dictionary<UInt32, String> l_pseudoInstructionMap = new Dictionary<UInt32, String>();

                    bool l_useIntegratedEcallHandler = Properties.Settings.Default.emulator_useIntegratedEcallHandler;
                    UInt32 l_ecallBase = Properties.Settings.Default.emulator_ecallBase;

                    m_rvEmulator.Assemble(l_code, l_labelMap, l_pseudoInstructionMap, l_useIntegratedEcallHandler, l_ecallBase);

                    if (Properties.Settings.Default.logging_enable)
                    {
                        m_logger.LogText("Compilation succesful:", false);
                        m_logger.LogCurrentTime(true);
                        m_logger.NewLine();
                        m_logger.UpdateLogUI();

                        m_logger.LogText(String.Format("Step {0:00000000}:", m_stepCount), true);
                    }
                    ++m_stepCount;

                    m_rvEmulator.ResetProcessor();

                    if (Properties.Settings.Default.logging_enable)
                    {
                        m_logger.NewLine();
                        m_logger.UpdateLogUI();
                    }

                    Delegate l_compilationFinishedDelegate = new Action(
                    () => {
                        m_simulationJustStarted = true;
                        m_simulationJustPaused = false;
                        m_breakpointHit = false;

                        m_editor.SetEditable(true);
                        m_processor.SetLabelReferences(l_labelMap);
                        m_processor.SetPseudoInstructionReference(l_pseudoInstructionMap);
                        m_processor.UpdateInfo();
                        m_processor.HighlightingEnabled = true;

                        m_compiling = false;
                        m_wasCompiled = true;

                        ChangePerspective(Perspective.Emulator);
                    });

                    Dispatcher.BeginInvoke(l_compilationFinishedDelegate);
                }
                catch (Exception e_assemblyException)
                {
                    Delegate l_exceptionDelegate = new Action(
                    () => {
                        ChangePerspective(Perspective.Editor);

                        if (e_assemblyException.GetType() == typeof(RVAssemblyException))
                        {
                            RVAssemblyException l_assemblyError = (RVAssemblyException)e_assemblyException;

                            if (Properties.Settings.Default.logging_enable)
                            {
                                m_logger.LogText("Compilation failed! Line " + l_assemblyError.Line + ", Column " + l_assemblyError.Column + ": \"" + l_assemblyError.Message + "\"", true);
                                m_logger.UpdateLogUI();
                            }

                            MessageBox.Show("L: " + l_assemblyError.Line + "; C: " + l_assemblyError.Column + "\n" + l_assemblyError.Message, "Compilation error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else if (e_assemblyException.GetType() == typeof(RVMemoryException))
                        {
                            RVMemoryException l_memoryError = (RVMemoryException)e_assemblyException;

                            if (Properties.Settings.Default.logging_enable)
                            {
                                m_logger.LogText(String.Format("Compilation failed! Trying to place code at undefined memory address: 0x{0,8:X8}", l_memoryError.FaultingAddress), true);
                                m_logger.UpdateLogUI();
                            }

                            MessageBox.Show(String.Format("Trying to place code at undefined memory address: 0x{0,8:X8}", l_memoryError.FaultingAddress), "Compilation error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        
                        m_editor.SetEditable(true);

                        if (Properties.Settings.Default.logging_enable)
                        {
                            m_logger.LogText("Simulation ended:", false);
                            m_logger.LogCurrentTime(true);
                            m_logger.NewLine();
                            m_logger.NewLine();
                            m_logger.UpdateLogUI();
                        }

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
            if (Properties.Settings.Default.logging_enable)
            {
                if (Properties.Settings.Default.logging_dontLogEcall == false || m_rvEmulator.HandlingTrap() != RVVector.ECALL)
                {
                    m_logger.LogText(String.Format("Step {0:00000000}:", m_stepCount), true);
                }
            }
            ++m_stepCount;

            m_rvEmulator.SingleStep();

            if (Properties.Settings.Default.logging_enable)
            {
                if (Properties.Settings.Default.logging_dontLogEcall == false || m_rvEmulator.HandlingTrap() != RVVector.ECALL)
                {
                    m_logger.NewLine();
                }

                m_logger.UpdateLogUI();
            }

            m_simulationJustStarted = false;
            m_simulationJustPaused = false;
            m_breakpointHit = false;

            if (m_rvEmulator.Halted)
            {
                m_simulationRunning = false;
                m_processor.UpdateInfo();

                if (Properties.Settings.Default.logging_enable)
                {
                    m_logger.LogText("Simulation ended:", false);
                    m_logger.LogCurrentTime(false);
                    m_logger.LogText("(Core halted)", true);
                    m_logger.NewLine();
                    m_logger.NewLine();
                    m_logger.UpdateLogUI();
                }

                MessageBox.Show("Simulation stopped.\nCore halted: " + m_rvEmulator.HaltReason, "Core halted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                m_processor.UpdateInfo();
            }
        }

        public void RunClocked()
        {
            m_runningClocked = true;
            m_clockTimer.Start();

            m_simulationJustStarted = false;
            m_simulationJustPaused = false;
            m_breakpointHit = false;
        }

        public void Run()
        {
            m_runningFast = true;

            m_simulationJustStarted = false;
            m_simulationJustPaused = false;
            m_breakpointHit = false;

            m_processor.UpdateInfo();
            m_processor.HighlightingEnabled = false;

            ThreadStart l_runThreadFunction = new ThreadStart(RunFastSimulation);

            m_simulationThread = new Thread(l_runThreadFunction);
            m_simulationThread.Start();
        }

        public void Pause()
        {
            m_simulationJustPaused = true;

            if (m_runningClocked)
            {
                m_clockTimer.Stop();
                m_runningClocked = false;

                m_processor.UpdateInfo();
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

                if (Properties.Settings.Default.logging_enable)
                {
                    m_logger.UpdateLogUI();
                }
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

            m_simulationJustStarted = false;
            m_simulationJustPaused = false;
            m_breakpointHit = false;

            m_rvEmulator.Halted = true;

            if (Properties.Settings.Default.logging_enable)
            {
                m_logger.LogText("Simulation ended:", false);
                m_logger.LogCurrentTime(false);
                m_logger.LogText("(Stopped by user)", true);
                m_logger.NewLine();
                m_logger.NewLine();
                m_logger.UpdateLogUI();
            }

            m_processor.UpdateInfo();
        }

        public void OpenInjectInterruptUI()
        {
            InjectInterruptWindow l_injectInterruptUI = new InjectInterruptWindow(GetFileName(), m_rvEmulator);
            l_injectInterruptUI.ShowDialog();

            if (m_runningFast == false)
            {
                m_processor.UpdateInfo();
            }
        }

        public void OpenTerminalPeripheralUI()
        {
            if (m_terminalWindowHandle == null)
            {
                m_terminalWindowHandle = new TerminalWindow(m_terminalPeripheral);

                String l_simulationName = GetFileName();
                if (l_simulationName == null)
                {
                    l_simulationName = "*untitled simulation*";
                }
                m_terminalWindowHandle.Title = "Terminal for " + l_simulationName;
            }

            m_terminalWindowHandle.Show();
            m_terminalWindowHandle.Focus();
        }

        public void OpenIOPanelPeripheralUI()
        {
            if (m_IOPanelWindowHandle == null)
            {
                m_IOPanelWindowHandle = new IOPanelWindow(m_IOPeripheral);

                String l_simulationName = GetFileName();
                if (l_simulationName == null)
                {
                    l_simulationName = "*untitled simulation*";
                }
                m_IOPanelWindowHandle.Title = "I/O panel for " + l_simulationName;
            }

            m_IOPanelWindowHandle.Show();
            m_IOPanelWindowHandle.Focus();
        }

        public void CloseAllPeripheralWindows()
        {
            m_terminalWindowHandle?.Close(true);
            m_IOPanelWindowHandle?.Close(true);

            m_terminalWindowHandle = null;
            m_IOPanelWindowHandle = null;
        }

        private void ClockTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(
            () => {
                if (Properties.Settings.Default.logging_enable)
                {
                    if (Properties.Settings.Default.logging_dontLogEcall == false || m_rvEmulator.HandlingTrap() != RVVector.ECALL)
                    {
                        m_logger.LogText(String.Format("Step {0:00000000}:", m_stepCount), true);
                    }
                }
                ++m_stepCount;

                m_rvEmulator.SingleStep();

                if (Properties.Settings.Default.logging_enable)
                {
                    if (Properties.Settings.Default.logging_dontLogEcall == false || m_rvEmulator.HandlingTrap() != RVVector.ECALL)
                    {
                        m_logger.NewLine();
                    }

                    m_logger.UpdateLogUI();
                }

                if (m_rvEmulator.Halted)
                {
                    m_clockTimer.Stop();

                    m_simulationRunning = false;
                    m_runningClocked = false;
                    m_processor.UpdateInfo();

                    if (Properties.Settings.Default.logging_enable)
                    {
                        m_logger.LogText("Simulation ended:", false);
                        m_logger.LogCurrentTime(false);
                        m_logger.LogText("(Core halted)", true);
                        m_logger.NewLine();
                        m_logger.NewLine();
                        m_logger.UpdateLogUI();
                    }

                    MessageBox.Show("Simulation stopped.\nCore halted: " + m_rvEmulator.HaltReason, "Core halted", MessageBoxButton.OK, MessageBoxImage.Information);

                    CommandManager.InvalidateRequerySuggested();
                }
                else if (m_rvEmulator.BreakpointHit())
                {
                    m_clockTimer.Stop();
                    m_runningClocked = false;

                    m_breakpointHit = true;

                    m_processor.UpdateInfo();

                    CommandManager.InvalidateRequerySuggested();
                }
                else
                {
                    m_processor.UpdateInfo();
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

                if (Properties.Settings.Default.logging_enable)
                {
                    if (Properties.Settings.Default.logging_dontLogEcall == false || m_rvEmulator.HandlingTrap() != RVVector.ECALL)
                    {
                        m_logger.LogText(String.Format("Step {0:00000000}:", m_stepCount), true);
                    }
                }
                ++m_stepCount;

                m_rvEmulator.SingleStep();

                if (Properties.Settings.Default.logging_enable)
                {
                    if (Properties.Settings.Default.logging_dontLogEcall == false || m_rvEmulator.HandlingTrap() != RVVector.ECALL)
                    {
                        m_logger.NewLine();
                    }
                }

                if (m_rvEmulator.Halted)
                {
                    Delegate l_simulationEndedDelegate = new Action(
                    () => {
                        m_simulationRunning = false;
                        m_runningFast = false;

                        m_processor.UpdateInfo();
                        m_processor.HighlightingEnabled = true;

                        if (Properties.Settings.Default.logging_enable)
                        {
                            m_logger.LogText("Simulation ended:", false);
                            m_logger.LogCurrentTime(false);
                            m_logger.LogText("(Core halted)", true);
                            m_logger.NewLine();
                            m_logger.NewLine();
                            m_logger.UpdateLogUI();
                        }

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

                        m_breakpointHit = true;

                        m_processor.UpdateInfo();
                        m_processor.HighlightingEnabled = true;

                        if (Properties.Settings.Default.logging_enable)
                        {
                            m_logger.UpdateLogUI();
                        }

                        CommandManager.InvalidateRequerySuggested();
                    });

                    Dispatcher.BeginInvoke(l_breakpointHitDelegate);

                    return;
                }
            }
        }
    }
}
