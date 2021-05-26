using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for TerminalSettingsPanel.xaml
    /// </summary>
    public partial class TerminalSettingsPanel : UserControl
    {
        static Brush[] s_brushColorLookUpTable;
        static String[] s_brushNameLookUpTable;

        public TerminalSettingsPanel()
        {
            InitializeComponent();

            GenerateColorTable();
            GenerateNameTable();

            for (int i_colorIndex = 0; i_colorIndex < s_brushColorLookUpTable.Length; ++i_colorIndex)
            {
                comboBoxTextColor.Items.Add(new ColorSelectionItem(s_brushColorLookUpTable[i_colorIndex], s_brushNameLookUpTable[i_colorIndex]));
                comboBoxBackgroundColor.Items.Add(new ColorSelectionItem(s_brushColorLookUpTable[i_colorIndex], s_brushNameLookUpTable[i_colorIndex]));
            }
        }

        public void SetTextColorIndex(int index)
        {
            if (index >= 0 && index < comboBoxTextColor.Items.Count)
            {
                comboBoxTextColor.SelectedIndex = index;
            }
        }

        public void SetBackgroundColorIndex(int index)
        {
            if (index >= 0 && index < comboBoxBackgroundColor.Items.Count)
            {
                comboBoxBackgroundColor.SelectedIndex = index;
            }
        }

        public int GetTextColorIndex()
        {
            return comboBoxTextColor.SelectedIndex;
        }

        public int GetBackgroundColorIndex()
        {
            return comboBoxBackgroundColor.SelectedIndex;
        }

        public static void GenerateColorTable()
        {
            if (s_brushColorLookUpTable != null)
            {
                return;
            }

            s_brushColorLookUpTable = new Brush[31];

            s_brushColorLookUpTable[0] = Brushes.Black;
            s_brushColorLookUpTable[1] = Brushes.Blue;
            s_brushColorLookUpTable[2] = Brushes.Brown;
            s_brushColorLookUpTable[3] = Brushes.Cyan;
            s_brushColorLookUpTable[4] = Brushes.DarkBlue;
            s_brushColorLookUpTable[5] = Brushes.DarkCyan;
            s_brushColorLookUpTable[6] = Brushes.DarkGray;
            s_brushColorLookUpTable[7] = Brushes.DarkGreen;
            s_brushColorLookUpTable[8] = Brushes.DarkMagenta;
            s_brushColorLookUpTable[9] = Brushes.DarkOrange;
            s_brushColorLookUpTable[10] = Brushes.DarkRed;
            s_brushColorLookUpTable[11] = Brushes.DarkTurquoise;
            s_brushColorLookUpTable[12] = Brushes.DarkViolet;
            s_brushColorLookUpTable[13] = Brushes.Gold;
            s_brushColorLookUpTable[14] = Brushes.Gray;
            s_brushColorLookUpTable[15] = Brushes.Green;
            s_brushColorLookUpTable[16] = Brushes.LightBlue;
            s_brushColorLookUpTable[17] = Brushes.LightCyan;
            s_brushColorLookUpTable[18] = Brushes.LightGray;
            s_brushColorLookUpTable[19] = Brushes.LightGreen;
            s_brushColorLookUpTable[20] = Brushes.LightYellow;
            s_brushColorLookUpTable[21] = Brushes.Lime;
            s_brushColorLookUpTable[22] = Brushes.Magenta;
            s_brushColorLookUpTable[23] = Brushes.Orange;
            s_brushColorLookUpTable[24] = Brushes.Pink;
            s_brushColorLookUpTable[25] = Brushes.Purple;
            s_brushColorLookUpTable[26] = Brushes.Red;
            s_brushColorLookUpTable[27] = Brushes.Turquoise;
            s_brushColorLookUpTable[28] = Brushes.Violet;
            s_brushColorLookUpTable[29] = Brushes.White;
            s_brushColorLookUpTable[30] = Brushes.Yellow;
        }

        public static void GenerateNameTable()
        {
            if (s_brushNameLookUpTable != null)
            {
                return;
            }

            s_brushNameLookUpTable = new String[31];

            s_brushNameLookUpTable[0] = "Black";
            s_brushNameLookUpTable[1] = "Blue";
            s_brushNameLookUpTable[2] = "Brown";
            s_brushNameLookUpTable[3] = "Cyan";
            s_brushNameLookUpTable[4] = "Dark blue";
            s_brushNameLookUpTable[5] = "Dark cyan";
            s_brushNameLookUpTable[6] = "Dark gray";
            s_brushNameLookUpTable[7] = "Dark green";
            s_brushNameLookUpTable[8] = "Dark magenta";
            s_brushNameLookUpTable[9] = "Dark orange";
            s_brushNameLookUpTable[10] = "Dark red";
            s_brushNameLookUpTable[11] = "Dark turquoise";
            s_brushNameLookUpTable[12] = "Dark violet";
            s_brushNameLookUpTable[13] = "Gold";
            s_brushNameLookUpTable[14] = "Gray";
            s_brushNameLookUpTable[15] = "Green";
            s_brushNameLookUpTable[16] = "Light blue";
            s_brushNameLookUpTable[17] = "Light cyan";
            s_brushNameLookUpTable[18] = "Light gray";
            s_brushNameLookUpTable[19] = "Light green";
            s_brushNameLookUpTable[20] = "Light yellow";
            s_brushNameLookUpTable[21] = "Lime";
            s_brushNameLookUpTable[22] = "Magenta";
            s_brushNameLookUpTable[23] = "Orange";
            s_brushNameLookUpTable[24] = "Pink";
            s_brushNameLookUpTable[25] = "Purple";
            s_brushNameLookUpTable[26] = "Red";
            s_brushNameLookUpTable[27] = "Turquoise";
            s_brushNameLookUpTable[28] = "Violet";
            s_brushNameLookUpTable[29] = "White";
            s_brushNameLookUpTable[30] = "Yellow";
        }

        public static Brush GetColorByIndex(int index)
        {
            GenerateColorTable();

            if (index < 0 || index >= s_brushColorLookUpTable.Length)
            {
                return null;
            }

            return s_brushColorLookUpTable[index];
        }
    }
}
