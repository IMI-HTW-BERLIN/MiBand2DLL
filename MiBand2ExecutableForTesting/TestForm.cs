using System;
using System.Windows.Forms;
using MiBand2DLL;

namespace MiBand2ExecutableForTesting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void GetHRButtonClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Connecting to band...");
            if(await MiBand2.ConnectToBand())
                System.Diagnostics.Debug.WriteLine("Connected");
            else
                System.Diagnostics.Debug.WriteLine("Could not connect! :(");
            
            System.Diagnostics.Debug.WriteLine("Getting heart beat...");
            int heartRate = await MiBand2.GetHeartRateAsync();
            
            System.Diagnostics.Debug.WriteLine("Current heart rate: " + heartRate);
        }
    }
}