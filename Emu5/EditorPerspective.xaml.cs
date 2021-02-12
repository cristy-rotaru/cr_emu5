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

        IHighlightingDefinition m_syntaxHighlightingDefinition = null;

        bool m_unsaved = true;
        String m_fileName = null;

        public EditorPerspective()
        {
            InitializeComponent();

            ActivateSyntaxHighighting();
        }

        private void ActivateSyntaxHighighting()
        {
            if (m_syntaxHighlightingDefinition == null)
            {
                Stream l_resourceStream = new MemoryStream(Properties.Resources.SyntaxHighlighter);
                XmlTextReader l_streamReder = new XmlTextReader(l_resourceStream);

                m_syntaxHighlightingDefinition = HighlightingLoader.Load(l_streamReder, HighlightingManager.Instance);
            }

            textEditorMain.SyntaxHighlighting = m_syntaxHighlightingDefinition;
        }

        public void RegisterFileModifiedCallback(FileModifiedDelegate handler)
        {
            m_textChangedHandler = handler;
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

        private void textEditorMain_TextChanged(object sender, EventArgs e)
        {
            m_unsaved = true;
            m_textChangedHandler?.Invoke();
        }
    }
}
