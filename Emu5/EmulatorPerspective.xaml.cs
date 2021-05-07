using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for EmulatorPerspective.xaml
    /// </summary>
    public partial class EmulatorPerspective : UserControl
    {
        RVEmulator m_emulator = null;
        PerspectivePage m_parentPage = null;

        UInt32 m_currentPC;
        UInt32[] m_previousRegisterValues;
        TextBlock[] m_registerTextBoxes;

        InstructionView m_instructionView;
        DataView m_memoryView;
        StackView m_stackView;

        public bool HighlightingEnabled
        {
            set
            {
                m_instructionView.HighlightingEnabled = value;
            }
        }

        public EmulatorPerspective(PerspectivePage parent)
        {
            InitializeComponent();

            m_parentPage = parent;

            m_instructionView = new InstructionView();
            m_memoryView = new DataView();
            m_stackView = new StackView();

            contentControlLeftPanel.Content = m_instructionView;
            //contentControlRightPanel.Content = m_memoryView;
            contentControlRightPanel.Content = m_stackView;

            m_currentPC = 0x0;
            m_previousRegisterValues = new UInt32[32];
            m_registerTextBoxes = new TextBlock[32];

            for (int i_index = 0; i_index < 32; ++i_index)
            {
                m_previousRegisterValues[i_index] = 0x0;
            }

            m_registerTextBoxes[0] = textBlockRegisterX0;
            m_registerTextBoxes[1] = textBlockRegisterX1;
            m_registerTextBoxes[2] = textBlockRegisterX2;
            m_registerTextBoxes[3] = textBlockRegisterX3;
            m_registerTextBoxes[4] = textBlockRegisterX4;
            m_registerTextBoxes[5] = textBlockRegisterX5;
            m_registerTextBoxes[6] = textBlockRegisterX6;
            m_registerTextBoxes[7] = textBlockRegisterX7;
            m_registerTextBoxes[8] = textBlockRegisterX8;
            m_registerTextBoxes[9] = textBlockRegisterX9;
            m_registerTextBoxes[10] = textBlockRegisterX10;
            m_registerTextBoxes[11] = textBlockRegisterX11;
            m_registerTextBoxes[12] = textBlockRegisterX12;
            m_registerTextBoxes[13] = textBlockRegisterX13;
            m_registerTextBoxes[14] = textBlockRegisterX14;
            m_registerTextBoxes[15] = textBlockRegisterX15;
            m_registerTextBoxes[16] = textBlockRegisterX16;
            m_registerTextBoxes[17] = textBlockRegisterX17;
            m_registerTextBoxes[18] = textBlockRegisterX18;
            m_registerTextBoxes[19] = textBlockRegisterX19;
            m_registerTextBoxes[20] = textBlockRegisterX20;
            m_registerTextBoxes[21] = textBlockRegisterX21;
            m_registerTextBoxes[22] = textBlockRegisterX22;
            m_registerTextBoxes[23] = textBlockRegisterX23;
            m_registerTextBoxes[24] = textBlockRegisterX24;
            m_registerTextBoxes[25] = textBlockRegisterX25;
            m_registerTextBoxes[26] = textBlockRegisterX26;
            m_registerTextBoxes[27] = textBlockRegisterX27;
            m_registerTextBoxes[28] = textBlockRegisterX28;
            m_registerTextBoxes[29] = textBlockRegisterX29;
            m_registerTextBoxes[30] = textBlockRegisterX30;
            m_registerTextBoxes[31] = textBlockRegisterX31;

            UpdateInfo();
        }

        public void BindEmulator(RVEmulator emulator)
        {
            m_emulator = emulator;

            m_instructionView.BindEmulator(m_emulator);
            m_memoryView.BindEmulator(m_emulator);
            m_stackView.BindEmulator(m_emulator);
        }

        public void SetLabelReferences(RVLabelReferenceMap labelMap)
        {
            m_instructionView.SetLabelReferences(labelMap);
        }

        public void SetPseudoInstructionReference(Dictionary<UInt32, String> pseudoInstructionMap)
        {
            m_instructionView.SetPseudoInstructionReference(pseudoInstructionMap);
        }

        public void UpdateInfo()
        {
            if (m_emulator == null)
            {
                for (int i_index = 0; i_index < 32; ++i_index)
                {
                    m_previousRegisterValues[i_index] = 0x0;

                    m_registerTextBoxes[i_index].Text = "";
                    m_registerTextBoxes[i_index].Foreground = Brushes.Black;
                }

                textBlockRegisterPC.Text = "";

                borderSimulationStatusBackground.Background = Brushes.White;
                textBlockSimulationStatus.Text = "";
            }
            else
            {
                UInt32[] l_registerValues = m_emulator.GetRegisterFile();

                for (int i_index = 0; i_index < 32; ++i_index)
                {
                    m_registerTextBoxes[i_index].Foreground = m_previousRegisterValues[i_index] == l_registerValues[i_index] ? Brushes.Black : Brushes.Red;
                    m_registerTextBoxes[i_index].Text = String.Format("0x{0,8:X8}", l_registerValues[i_index]);
                    m_previousRegisterValues[i_index] = l_registerValues[i_index];
                }

                m_currentPC = m_emulator.GetProgramCounter();
                textBlockRegisterPC.Text = String.Format("0x{0,8:X8}", m_currentPC);

                UpdateSimulationStatus();
            }

            m_instructionView.UpdateInfo();
            m_memoryView.UpdateInfo();
            m_stackView.UpdateInfo();
        }

        private void UpdateSimulationStatus()
        {
            if (m_parentPage.IsRunning == false)
            {
                borderSimulationStatusBackground.Background = Brushes.White;
                textBlockSimulationStatus.Text = "Simulation stopped";
            }
            else if (m_parentPage.JustStarted)
            {
                borderSimulationStatusBackground.Background = Brushes.White;
                textBlockSimulationStatus.Text = "Simulation started";
            }
            else if (m_parentPage.JustPaused)
            {
                borderSimulationStatusBackground.Background = Brushes.White;
                textBlockSimulationStatus.Text = "Simulation paused";
            }
            else if (m_parentPage.RunningFastSimulation)
            {
                borderSimulationStatusBackground.Background = Brushes.LightGreen;
                textBlockSimulationStatus.Text = "Running";
            }
            else if (m_parentPage.BreakpointHit)
            {
                borderSimulationStatusBackground.Background = Brushes.Yellow;
                textBlockSimulationStatus.Text = "Breakpoint hit";
            }
            else
            {
                RVVector? l_aboutToTake, l_handling;

                if ((l_aboutToTake = m_emulator.AboutToTakeInterrupt()) != null)
                {
                    String l_status = "About to ";
                    if (l_aboutToTake == RVVector.Reset)
                    {
                        l_status += "be reset";
                    }
                    else if (l_aboutToTake == RVVector.NMI)
                    {
                        l_status += "take NMI";
                    }
                    else
                    {
                        l_status += "take external int " + (int)l_aboutToTake;
                    }

                    borderSimulationStatusBackground.Background = Brushes.LightBlue;
                    textBlockSimulationStatus.Text = l_status;
                }
                else if (m_emulator.WaitingForInterrupt())
                {
                    borderSimulationStatusBackground.Background = Brushes.LightBlue;
                    textBlockSimulationStatus.Text = "Waiting for interrupt";
                }
                else if ((l_handling = m_emulator.HandlingTrap()) != null)
                {
                    String l_status = "Handling ";

                    switch (l_handling)
                    {
                        case RVVector.NMI:
                        {
                            l_status += "NMI";
                        }
                        break;

                        case RVVector.ECALL:
                        {
                            l_status += "ECALL";
                        }
                        break;

                        case RVVector.MisalignedPC:
                        {
                            l_status += "misaligned PC fault";
                        }
                        break;

                        case RVVector.MisalignedMemory:
                        {
                            l_status += "misaligned memory access fault";
                        }
                        break;

                        case RVVector.UndefinedMemory:
                        {
                            l_status += "undefined memory space fault";
                        }
                        break;

                        case RVVector.InvalidInstruction:
                        {
                            l_status += "invalid instruction fault";
                        }
                        break;

                        case RVVector.DivisionBy0:
                        {
                            l_status += "division by 0 fault";
                        }
                        break;

                        default:
                        {
                            l_status += "external int " + (int)l_handling;
                        }
                        break;
                    }

                    borderSimulationStatusBackground.Background = Brushes.LightBlue;
                    textBlockSimulationStatus.Text = l_status;
                }
                else
                {
                    borderSimulationStatusBackground.Background = Brushes.LightGreen;
                    textBlockSimulationStatus.Text = "Normal operation";
                }
            }
        }
    }
}
