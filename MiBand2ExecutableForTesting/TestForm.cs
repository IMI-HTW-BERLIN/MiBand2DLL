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

        private async void ConnectBandButtonClicked(object sender, EventArgs e) => await MiBand2.ConnectToBand();

        private async void GetHRButtonClicked(object sender, EventArgs e)
        {
            await MiBand2.StartHeartRateMeasureContinuous();
            MiBand2.SubscribeToHeartRateChange(OnHeartRateChange);

            void OnHeartRateChange(int newHeartRate)
            {
                textBox1.Text = newHeartRate.ToString();
            }
        }

        private async void AskForTouchButtonClicked(object sender, EventArgs e) => await MiBand2.AskUserForTouch();

        private async void StopMeasurementButtonClicked(object sender, EventArgs e) =>
            await MiBand2.StopAllMeasurements();
    }
}