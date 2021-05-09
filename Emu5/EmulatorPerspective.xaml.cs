using System;
using System.Collections.Generic;
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
        Border[] m_viewOrder;

        InstructionView m_instructionView;
        DataView m_memoryView;
        StackView m_stackView;

        bool m_viewSelectionMouseDown, m_viewSelectionMouseMoved;
        double m_viewSelectionMouseDelta;
        int m_viewSelectionIndex;

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
            contentControlRightPanel.Content = m_memoryView;
            //contentControlRightPanel.Content = m_stackView;

            m_currentPC = 0x0;
            m_previousRegisterValues = new UInt32[32];
            m_registerTextBoxes = new TextBlock[32];

            m_viewOrder = new Border[3];
            m_viewOrder[0] = borderInstructionView;
            m_viewOrder[1] = borderMemoryView;
            m_viewOrder[2] = borderStackView;

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

            m_viewSelectionMouseDown = false;
            m_viewSelectionMouseMoved = false;
            m_viewSelectionMouseDelta = 0;
            m_viewSelectionIndex = -1;

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

        private void canvasViewSelector_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (m_viewOrder == null)
            {
                return;
            }

            UpdateCanvasLayout();
        }

        private void borderView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            m_viewSelectionMouseDown = true;
            m_viewSelectionMouseMoved = false;

            m_viewSelectionMouseDelta = Mouse.GetPosition(canvasViewSelector).X - Canvas.GetLeft((UIElement)sender);

            for (int i_viewSelectorIndex = 0; i_viewSelectorIndex < 3; ++i_viewSelectorIndex)
            {
                if (m_viewOrder[i_viewSelectorIndex] == sender)
                {
                    m_viewSelectionIndex = i_viewSelectorIndex;
                    Canvas.SetZIndex(m_viewOrder[i_viewSelectorIndex], 1); // bring to front the element that was clicked
                }
                else
                {
                    Canvas.SetZIndex(m_viewOrder[i_viewSelectorIndex], 0);
                }
            }
        }

        private void canvasViewSelector_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (m_viewSelectionMouseDown == false)
            {
                return;
            }

            if (m_viewSelectionMouseMoved)
            {
                UpdateViewOrder();
            }
            else
            {
                if (m_viewSelectionIndex == 2)
                {
                    if (m_viewOrder[2] == borderInstructionView) // instruction view prefers left panel
                    {
                        Border l_swap = m_viewOrder[2];
                        m_viewOrder[2] = m_viewOrder[0];
                        m_viewOrder[0] = l_swap;

                        contentControlLeftPanel.Content = m_instructionView;
                    }
                    else if (m_viewOrder[2] == borderMemoryView) // memory view prefers right panel
                    {
                        Border l_swap = m_viewOrder[2];
                        m_viewOrder[2] = m_viewOrder[1];
                        m_viewOrder[1] = l_swap;

                        contentControlRightPanel.Content = m_memoryView;
                    }
                    else if (m_viewOrder[2] == borderStackView) // stack view prefers right panel
                    {
                        Border l_swap = m_viewOrder[2];
                        m_viewOrder[2] = m_viewOrder[1];
                        m_viewOrder[1] = l_swap;

                        contentControlRightPanel.Content = m_stackView;
                    }
                }
            }

            UpdateCanvasLayout();

            m_viewSelectionMouseDown = false;
            m_viewSelectionMouseMoved = false;
            m_viewSelectionMouseDelta = 0;
            m_viewSelectionIndex = -1;
        }

        private void canvasViewSelector_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_viewSelectionMouseDown == false)
            {
                return;
            }

            m_viewSelectionMouseMoved = true;

            double l_leftPosition = Mouse.GetPosition(canvasViewSelector).X - m_viewSelectionMouseDelta;
            if (l_leftPosition < 2)
            {
                l_leftPosition = 2;
            }
            else if (l_leftPosition + 2 + m_viewOrder[m_viewSelectionIndex].ActualWidth > canvasViewSelector.ActualWidth)
            {
                l_leftPosition = canvasViewSelector.ActualWidth - m_viewOrder[m_viewSelectionIndex].ActualWidth - 2;
            }

            // check if index must change
            bool l_updateCanvasLayoutRequired = false;
            switch (m_viewSelectionIndex)
            {
                case 0:
                {
                    if (l_leftPosition > Canvas.GetLeft(m_viewOrder[1]))
                    {
                        Border l_swap = m_viewOrder[0];
                        m_viewOrder[0] = m_viewOrder[1];
                        m_viewOrder[1] = l_swap;

                        m_viewSelectionIndex = 1;

                        l_updateCanvasLayoutRequired = true;
                    }
                    if (l_leftPosition > Canvas.GetLeft(m_viewOrder[2]))
                    {
                        Border l_swap = m_viewOrder[1];
                        m_viewOrder[1] = m_viewOrder[2];
                        m_viewOrder[2] = l_swap;

                        m_viewSelectionIndex = 2;
                    }
                }
                break;

                case 1:
                {
                    if (l_leftPosition < Canvas.GetLeft(m_viewOrder[0]))
                    {
                        Border l_swap = m_viewOrder[1];
                        m_viewOrder[1] = m_viewOrder[0];
                        m_viewOrder[0] = l_swap;

                        m_viewSelectionIndex = 0;

                        l_updateCanvasLayoutRequired = true;
                    }
                    else if (l_leftPosition > Canvas.GetLeft(m_viewOrder[2]))
                    {
                        Border l_swap = m_viewOrder[1];
                        m_viewOrder[1] = m_viewOrder[2];
                        m_viewOrder[2] = l_swap;

                        m_viewSelectionIndex = 2;

                        l_updateCanvasLayoutRequired = true;
                    }
                }
                break;

                case 2:
                {
                    if (l_leftPosition < Canvas.GetLeft(m_viewOrder[1]))
                    {
                        Border l_swap = m_viewOrder[2];
                        m_viewOrder[2] = m_viewOrder[1];
                        m_viewOrder[1] = l_swap;

                        m_viewSelectionIndex = 1;

                        l_updateCanvasLayoutRequired = true;
                    }
                    if (l_leftPosition < Canvas.GetLeft(m_viewOrder[0]))
                    {
                        Border l_swap = m_viewOrder[1];
                        m_viewOrder[1] = m_viewOrder[0];
                        m_viewOrder[0] = l_swap;

                        m_viewSelectionIndex = 0;
                    }
                }
                break;
            }

            if (l_updateCanvasLayoutRequired)
            {
                UpdateCanvasLayout();
            }

            Canvas.SetLeft(m_viewOrder[m_viewSelectionIndex], l_leftPosition);
        }

        private void canvasViewSelector_MouseLeave(object sender, MouseEventArgs e)
        {
            if (m_viewSelectionMouseDown == false)
            {
                return;
            }

            UpdateViewOrder();

            m_viewSelectionMouseDown = false;
            m_viewSelectionMouseMoved = false;
            m_viewSelectionMouseDelta = 0;
            m_viewSelectionIndex = -1;

            UpdateCanvasLayout();
        }

        private void UpdateCanvasLayout()
        {
            double l_view0Left = canvasViewSelector.ActualWidth / 4 - m_viewOrder[0].ActualWidth / 2;
            double l_view1Left = canvasViewSelector.ActualWidth / 2 - m_viewOrder[1].ActualWidth / 2;
            double l_view2Left = canvasViewSelector.ActualWidth / 4 * 3 - m_viewOrder[2].ActualWidth / 2;

            double l_rectangleLeft = l_view0Left - 4;
            double l_rectangleWidth = l_view1Left - l_view0Left + m_viewOrder[1].ActualWidth + 8;

            Canvas.SetLeft(m_viewOrder[0], l_view0Left);
            Canvas.SetLeft(m_viewOrder[1], l_view1Left);
            Canvas.SetLeft(m_viewOrder[2], l_view2Left);
            Canvas.SetLeft(rectangleSelectedViews, l_rectangleLeft);

            rectangleSelectedViews.Width = l_rectangleWidth;
        }

        private void UpdateViewOrder()
        {
            contentControlLeftPanel.Content = null;
            contentControlRightPanel.Content = null;

            if (m_viewOrder[0] == borderInstructionView)
            {
                contentControlLeftPanel.Content = m_instructionView;
            }
            else if (m_viewOrder[0] == borderMemoryView)
            {
                contentControlLeftPanel.Content = m_memoryView;
            }
            else if (m_viewOrder[0] == borderStackView)
            {
                contentControlLeftPanel.Content = m_stackView;
            }

            if (m_viewOrder[1] == borderInstructionView)
            {
                contentControlRightPanel.Content = m_instructionView;
            }
            else if (m_viewOrder[1] == borderMemoryView)
            {
                contentControlRightPanel.Content = m_memoryView;
            }
            else if (m_viewOrder[1] == borderStackView)
            {
                contentControlRightPanel.Content = m_stackView;
            }
        }
    }
}
