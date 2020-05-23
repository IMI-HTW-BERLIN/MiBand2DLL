using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using MiBand2DLL.CustomExceptions;
using MiBand2DLL.lib;
using MiBand2DLL.util;

// REVIEW: Do we need MonoBehaviour in here?
// using UnityEngine;

namespace MiBand2DLL
{
    public static class MiBand2
    {
        public static bool Connected =>
            _connectedBtDevice != null &&
            _connectedBtDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;


        public static bool Authenticated => Identity.Authenticated;

        public static event Delegates.ConnectionStatusDelegate DeviceConnectionChanged;


        private static readonly HeartRate HeartRate = new HeartRate();
        private static readonly Identity Identity = new Identity();

        private static BluetoothLEDevice _connectedBtDevice;

        public static async Task ConnectToDevice(string name = Consts.General.MI_BAND_NAME)
        {
            DeviceInformationCollection devices =
                await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());

            DeviceInformation device = devices.FirstOrDefault(information => information.Name == "MI Band 2");
            if (device == null)
                throw new DeviceNotFoundException("Couldn't find device. Is it paired?");

            _connectedBtDevice = await BluetoothLEDevice.FromIdAsync(device.Id);

            if (_connectedBtDevice == null)
                throw new CustomException("Couldn't get BluetoothLEDevice from DeviceInformation. " +
                                          "Debugging required if this happens.");

            _connectedBtDevice.ConnectionStatusChanged += ConnectionStatusChanged;
        }

        public static async Task DisconnectDevice()
        {
            await HeartRate.Dispose();
            Identity.Dispose();
            _connectedBtDevice?.Dispose();
            _connectedBtDevice = null;
            GC.Collect();
        }

        public static async Task AuthenticateBand() => await Identity.AuthenticateAsync();

        public static async Task InitializeAuthenticationFunctionality() =>
            await Identity.Initialize(_connectedBtDevice);

        public static async Task InitializeHeartRateFunctionality()
        {
            if (IsConnectedAndAuthorized())
                await HeartRate.Initialize(_connectedBtDevice);
        }

        /// <summary>
        /// Measures the heart rate ONCE.
        /// </summary>
        /// <returns>The measured heart rate.</returns>
        public static async Task<int> GetSingleHeartRate()
        {
            if (!Connected || !Authenticated)
                return 0;

            await HeartRate.StartSingleHeartRateMeasurementAsync();
            return HeartRate.LastHeartRate;
        }

        public static async Task StartHeartRateMeasureContinuous()
        {
            if (IsConnectedAndAuthorized())
                await HeartRate.StartContinuousHeartRateMeasurementAsync();
        }

        public static async Task StopAllMeasurements()
        {
            if (IsConnectedAndAuthorized())
                await HeartRate.StopAllMeasurements();
        }

        public static void SubscribeToHeartRateChange(Delegates.HeartRateDelegate method) =>
            HeartRate.OnHeartRateChange += method;

        public static async Task AskUserForTouch()
        {
            if (!Connected)
                throw new DeviceDisconnectedException("The device is currently not connected.");

            await Identity.AskForUserTouch();
        }

        private static async void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (!Connected)
                await DisconnectDevice();
            DeviceConnectionChanged?.Invoke(Connected);
        }

        private static bool IsConnectedAndAuthorized()
        {
            if (!Connected)
                throw new DeviceDisconnectedException("The device is currently not connected.");

            if (!Authenticated)
                throw new NotAuthenticatedException("The device is currently not authorized. " +
                                                    "Make sure to authorize the band before using any functionality");
            return true;
        }
    }
}