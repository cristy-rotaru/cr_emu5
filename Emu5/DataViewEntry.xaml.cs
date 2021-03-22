using System;
using System.Windows.Controls;

namespace Emu5
{
    /// <summary>
    /// Interaction logic for DataViewEntry.xaml
    /// </summary>
    public partial class DataViewEntry : UserControl
    {
        public DataViewEntry()
        {
            InitializeComponent();
        }

        public void DisplayData(String baseAddress, String data0, String data1, String data2, String data3, String data4, String data5, String data6, String data7)
        {
            textBlockBaseAddress.Text = baseAddress;

            textBlockData0.Text = data0;
            textBlockData1.Text = data1;
            textBlockData2.Text = data2;
            textBlockData3.Text = data3;
            textBlockData4.Text = data4;
            textBlockData5.Text = data5;
            textBlockData6.Text = data6;
            textBlockData7.Text = data7;
        }
    }
}
