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
    public class MiBand2SDK
    {
        public HeartRate HeartRate { get; } = new HeartRate();
        public Identity Identity { get; } = new Identity();


        /// <summary>
        /// Connect to paired device
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectAsync()
        {
            DeviceInformation device = await Identity.GetPairedBand();

            if (device == null)
                return false;

            Gatt.BluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
            return Gatt.BluetoothLeDevice != null;
        }

        /// <summary>
        /// Authenticates the connection to the band. Needed for receiving data and sending commands.
        /// </summary>
        /// <returns>Whether the authentication was successful</returns>
        public async Task<bool> AuthenticateAsync() => await Identity.AuthenticateAsync();
    }
}