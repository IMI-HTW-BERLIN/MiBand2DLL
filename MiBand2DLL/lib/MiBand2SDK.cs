using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace MiBand2DLL.lib
{
    /// TODO: Update/Add comments and summaries.
    /// TODO: Finish clean-up/refactoring.
    /// <summary>
    /// Some of the following code was taken, refactored and adjusted for our own purposes from:
    /// https://github.com/AL3X1/Mi-Band-2-SDK
    /// </summary>
    public class MiBand2SDk
    {
        public Identity Identity = new Identity();
        public HeartRate HeartRate = new HeartRate();

        /// <summary>
        /// Connect to paired device
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectAsync()
        {
            Identity auth = new Identity();
            DeviceInformation device = await auth.GetPairedBand();

            if (device != null)
            {
                Gatt.BluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
                return Gatt.BluetoothLeDevice != null;
            }

            return false;
        }

        /// <summary>
        /// Connect to device by id
        /// </summary>
        /// <param name="deviceInfo"></param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(DeviceInformation deviceInfo)
        {
            if (deviceInfo != null)
            {
                Gatt.BluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
                return Gatt.BluetoothLeDevice != null;
            }

            return false;
        }

        /// <summary>
        /// Unpair band from device
        /// </summary>
        public async Task UnpairDeviceAsync()
        {
            if (Gatt.BluetoothLeDevice != null)
                await Gatt.BluetoothLeDevice.DeviceInformation.Pairing.UnpairAsync();
        }

        public bool IsConnected() => Gatt.BluetoothLeDevice != null &&
                                     Gatt.BluetoothLeDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
    }
}