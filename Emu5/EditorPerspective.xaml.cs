using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for EditorPerspective.xaml
    /// </summary>
    public partial class EditorPerspective : UserControl
    {
        public delegate void FileModifiedDelegate();

        FileModifiedDelegate m_textChangedHandler = null;

        static IHighlightingDefinition s_syntaxHighlightingDefinition = null;

        bool m_unsaved = true;
        String m_fileName = null;

        public EditorPerspective()
        {
            InitializeComponent();

            ActivateSyntaxHighighting();
        }

        private void ActivateSyntaxHighighting()
        {
            if (s_syntaxHighlightingDefinition == null)
            {
                Stream l_resourceStream = new MemoryStream(Properties.Resources.SyntaxHighlighter);
                XmlTextReader l_streamReder = new XmlTextReader(l_resourceStream);

                s_syntaxHighlightingDefinition = HighlightingLoader.Load(l_streamReder, HighlightingManager.Instance);
            }

            textEditorMain.SyntaxHighlighting = s_syntaxHighlightingDefinition;
        }

        public void RegisterFileModifiedCallback(FileModifiedDelegate handler)
        {
            m_textChangedHandler = handler;
        }

        public void SetFontSize(int size)
        {
            if (size >= 6 && size <= 48)
            {
                textEditorMain.FontSize = size;
            }
        }

        public void SetSyntaxHighlightingEnabled(bool enabled)
        {
            textEditorMain.SyntaxHighlighting = enabled ? s_syntaxHighlightingDefinition : null;
        }

        public void SetEditable(bool editable)
        {
            textEditorMain.IsReadOnly = !editable;
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

        public bool Save()
        {
            if (m_unsaved == false)
            {
                return true;
            }

            if (m_fileName == null)
            {
                SaveFileDialog l_sfd = new SaveFileDialog();
                l_sfd.Filter = "Assembly file (*.asm, *.s)|*.asm;*.s|All files|*";
                l_sfd.FileName = "Untitled.asm";

                if (l_sfd.ShowDialog() == true)
                {
                    m_fileName = l_sfd.FileName;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                StreamWriter l_fileWriter = new StreamWriter(m_fileName, false, Encoding.Unicode);
                l_fileWriter.Write(textEditorMain.Text);

                l_fileWriter.Close();
            }
            catch (Exception e_fileSaveException)
            {
                MessageBox.Show(e_fileSaveException.Message, "Could not save the file", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            m_unsaved = false;
            return true;
        }

        public bool SaveAs()
        {
            String l_oldFileName = m_fileName;

            SaveFileDialog l_sfd = new SaveFileDialog();
            l_sfd.Filter = "Assembly file (*.asm, *.s)|*.asm;*.s|All files|*";
            l_sfd.FileName = m_fileName == null ? "Untitled.asm" : m_fileName;

            if (l_sfd.ShowDialog() == true)
            {
                m_fileName = l_sfd.FileName;
            }
            else
            {
                return false;
            }

            try
            {
                StreamWriter l_fileWriter = new StreamWriter(m_fileName, false, Encoding.Unicode);
                l_fileWriter.Write(textEditorMain.Text);

                l_fileWriter.Close();
            }
            catch (Exception e_fileSaveException)
            {
                m_fileName = l_oldFileName;

                MessageBox.Show(e_fileSaveException.Message, "Could not save the file", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            m_unsaved = false;
            return true;
        }

        public void GenerateTemplate(ProgramTemplate template, UInt32 codeBase, UInt32 initialStack, bool usingInternalEcall = false, UInt32 ecallBase = 0xFFFFF000, UInt32 dataBase = 0x1000)
        {
            textEditorMain.Clear();

            if (template == ProgramTemplate.Basic)
            {
                textEditorMain.AppendText("@0" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _program_start" + Environment.NewLine);

                if (usingInternalEcall)
                {
                    textEditorMain.AppendText("\tDW _NMI_handler" + Environment.NewLine);
                    textEditorMain.AppendText(String.Format("\tDW 0x{0,8:X8}", ecallBase) + Environment.NewLine);
                }

                textEditorMain.AppendText(Environment.NewLine);

                textEditorMain.AppendText(String.Format("@{0:X}", codeBase) + Environment.NewLine);
                textEditorMain.AppendText("_program_start:" + Environment.NewLine);
                textEditorMain.AppendText(String.Format("\tli sp, 0x{0:X}", initialStack) + Environment.NewLine);
                textEditorMain.AppendText("\t# your code goes here" + Environment.NewLine + Environment.NewLine + Environment.NewLine);

                if (usingInternalEcall)
                {
                    textEditorMain.AppendText("\taddi a0, zero, 1" + Environment.NewLine);
                    textEditorMain.AppendText("\tecall # stop simulation" + Environment.NewLine + Environment.NewLine);

                    textEditorMain.AppendText("_NMI_handler:" + Environment.NewLine);
                    textEditorMain.AppendText("\tiret");
                }
                else
                {
                    textEditorMain.AppendText("\thlt # stop simulation");
                }
            }
            else if (template == ProgramTemplate.Advanced)
            {
                textEditorMain.AppendText("@0" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _program_start" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _NMI_handler" + Environment.NewLine);

                if (usingInternalEcall)
                {
                    textEditorMain.AppendText(String.Format("\tDW 0x{0,8:X8}", ecallBase) + Environment.NewLine);
                }
                else
                {
                    textEditorMain.AppendText("\tDW _ECALL_handler" + Environment.NewLine);
                }

                textEditorMain.AppendText("\tDW _MisalignedPC_handler" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _MisalignedMemory_handler" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _UndefinedMemory_handler" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _InvalidInstruction_handler" + Environment.NewLine);
                textEditorMain.AppendText("\tDW _DivisionBy0_handler" + Environment.NewLine);

                for (int i_interrupt = 8; i_interrupt < 32; ++i_interrupt)
                {
                    textEditorMain.AppendText(String.Format("\tDW _int{0}_handler", i_interrupt) + Environment.NewLine);
                }

                textEditorMain.AppendText(Environment.NewLine);

                textEditorMain.AppendText(String.Format("@{0:X}", dataBase) + Environment.NewLine);
                textEditorMain.AppendText("\tDB 0, 1, 2, 3, 4, 5, 6, 7" + Environment.NewLine);

                textEditorMain.AppendText(Environment.NewLine);

                textEditorMain.AppendText(String.Format("@{0:X}", codeBase) + Environment.NewLine);
                textEditorMain.AppendText("_program_start:" + Environment.NewLine);
                textEditorMain.AppendText(String.Format("\tli sp, 0x{0:X}", initialStack) + Environment.NewLine);
                textEditorMain.AppendText("\t# your code goes here" + Environment.NewLine + Environment.NewLine + Environment.NewLine);

                if (usingInternalEcall)
                {
                    textEditorMain.AppendText("\taddi a0, zero, 1" + Environment.NewLine);
                    textEditorMain.AppendText("\tecall # stop simulation" + Environment.NewLine);
                }
                else
                {
                    textEditorMain.AppendText("\thlt # stop simulation");
                }

                textEditorMain.AppendText(Environment.NewLine);

                textEditorMain.AppendText("_NMI_handler:" + Environment.NewLine);
                textEditorMain.AppendText("\tiret" + Environment.NewLine);

                textEditorMain.AppendText(Environment.NewLine);

                if (usingInternalEcall == false)
                {
                    textEditorMain.AppendText("_ECALL_handler:" + Environment.NewLine);
                    textEditorMain.AppendText("\tiret" + Environment.NewLine);

                    textEditorMain.AppendText(Environment.NewLine);
                }

                textEditorMain.AppendText("_MisalignedPC_handler:" + Environment.NewLine);
                textEditorMain.AppendText("_MisalignedMemory_handler:" + Environment.NewLine);
                textEditorMain.AppendText("_UndefinedMemory_handler:" + Environment.NewLine);
                textEditorMain.AppendText("_InvalidInstruction_handler:" + Environment.NewLine);
                textEditorMain.AppendText("_DivisionBy0_handler:" + Environment.NewLine);
                textEditorMain.AppendText("\thlt" + Environment.NewLine);

                textEditorMain.AppendText(Environment.NewLine);

                for (int i_interrupt = 8; i_interrupt < 32; ++i_interrupt)
                {
                    textEditorMain.AppendText(String.Format("_int{0}_handler:", i_interrupt) + Environment.NewLine);
                }
                textEditorMain.AppendText("\tiret");
            }
        }

        public bool Open()
        {
            OpenFileDialog l_ofd = new OpenFileDialog();
            l_ofd.Filter = "Assembly file(*.asm, *.s) | *.asm; *.s | All files | *";

            if (l_ofd.ShowDialog() == true)
            {
                m_fileName = l_ofd.FileName;
            }
            else
            {
                return false;
            }

            try
            {
                StreamReader l_fileReader = new StreamReader(m_fileName);

                textEditorMain.Text = l_fileReader.ReadToEnd();
            }
            catch (Exception e_fileOpenException)
            {
                MessageBox.Show(e_fileOpenException.Message, "Could not open the file", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            m_unsaved = false;
            return true;
        }

        public bool IsUnsaved()
        {
            return m_unsaved;
        }

        public String GetFileName()
        {
            if (m_fileName == null)
            {
                return null;
            }

            return Path.GetFileName(m_fileName);
        }

        public String GetText()
        {
            String l_text = textEditorMain.Text;
            return l_text;
        }

        private void textEditorMain_TextChanged(object sender, EventArgs e)
        {
            m_unsaved = true;
            m_textChangedHandler?.Invoke();
        }
    }
}
