using System;
using System.Windows;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for TabHeader.xaml
    /// </summary>
    public partial class TabHeader : UserControl
    {
        public delegate void CloseTabDelegate();

        bool m_unsaved = false;
        String m_headerText = "";
        CloseTabDelegate m_closeTabHandler = null;

        public TabHeader()
        {
            InitializeComponent();
        }

        public TabHeader(String header, bool unsaved)
        {
            InitializeComponent();

            m_unsaved = unsaved;
            m_headerText = header;

            textBlockTabText.Text = m_unsaved ? m_headerText + "*" : m_headerText;
        }

        public TabHeader(String header, bool unsaved, CloseTabDelegate handler)
        {
            InitializeComponent();

            m_closeTabHandler = handler;
            m_unsaved = unsaved;
            m_headerText = header;

            textBlockTabText.Text = m_unsaved ? m_headerText + "*" : m_headerText;
        }

        public void ChangeHeaderText(String header, bool unsaved)
        {
            m_unsaved = unsaved;
            m_headerText = header;
            textBlockTabText.Text = m_unsaved ? m_headerText + "*" : m_headerText;
        }

        public void SetSavedState(bool unsaved)
        {
            m_unsaved = unsaved;
            textBlockTabText.Text = m_unsaved ? m_headerText + "*" : m_headerText;
        }

        public bool IsUnsaved()
        {
            return m_unsaved;
        }

        public void RegisterTabCloseCallback(CloseTabDelegate handler)
        {
            m_closeTabHandler = handler;
        }

        private void buttonCloseTab_Click(object sender, RoutedEventArgs e)
        {
            m_closeTabHandler?.Invoke();
        }
    }
}
