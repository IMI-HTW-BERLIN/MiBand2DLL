using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using MiBand2DLL;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await MiBand2.ConnectBandAsync();
            await Task.Delay(5000);
            await MiBand2.AuthenticateBandAsync();
            await Task.Delay(5000);
            await MiBand2.StartMeasurementAsync();
        }
    }
}