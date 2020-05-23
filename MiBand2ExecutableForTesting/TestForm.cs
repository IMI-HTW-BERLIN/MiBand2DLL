using System;
using System.Windows.Forms;
using MiBand2DLL;

namespace MiBand2ExecutableForTesting
{
    public partial class Form1 : Form
    {
        private int _numberOfChanges;

        public Form1()
        {
            InitializeComponent();
        }

        private async void ConnectBandButtonClicked(object sender, EventArgs e)
        {
            MiBand2.DeviceConnectionChanged += ConnectionStatusChanged;
            await MiBand2.ConnectToDevice();
            await MiBand2.InitializeAuthenticationFunctionality();
            await MiBand2.AuthenticateBand();
        }

        private async void DisconnectButtonClicked(object sender, EventArgs e)
        {
            MiBand2.DeviceConnectionChanged -= ConnectionStatusChanged;
            await MiBand2.DisconnectDevice();
        }

        private void CheckConnectionButtonClicked(object sender, EventArgs e)
        {
            textBox2.Text = MiBand2.Connected.ToString();
        }

        private void ConnectionStatusChanged(bool isConnected)
        {
            isConnectedTextBox.Text = isConnected.ToString();
            numberBox.Text = (++_numberOfChanges).ToString();
        }


        private async void GetHRButtonClicked(object sender, EventArgs e)
        {
            await MiBand2.InitializeHeartRateFunctionality();
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