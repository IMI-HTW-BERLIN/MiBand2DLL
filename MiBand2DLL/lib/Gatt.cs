using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace MiBand2DLL.lib
{
    /// TODO: Update/Add comments and summaries.
    /// <summary>
    /// Some of the following code was taken, refactored and adjusted for our own purposes from:
    /// https://github.com/AL3X1/Mi-Band-2-SDK
    /// </summary>
    public static class Gatt
    {
        /// <summary>
        /// Property, responsive to connection with band via GATT protocol.
        /// If device is not connected to phone, property will be null.
        /// </summary>
        public static BluetoothLEDevice bluetoothLEDevice { get; set; }

        /// <summary>
        /// Get GATT characteristic by service UUID.
        /// Service UUID can be taken from unofficial MIBand2-Protocol:
        /// https://github.com/aashari/mi-band-2/blob/master/README.md
        /// </summary>
        /// <param name="serviceUuid">GATT Service UUID</param>
        /// <param name="characteristicUuid">GATT Characteristic UUID</param>
        /// <returns>GattCharacteristic object if characteristic is exists, else returns null</returns>
        public static async Task<GattCharacteristic> GetCharacteristicByServiceUuid(Guid serviceUuid, Guid characteristicUuid)
        {
            if (bluetoothLEDevice == null)
                throw new Exception("Cannot get characteristic from service: Device is disconnected.");

            GattDeviceServicesResult service = await bluetoothLEDevice.GetGattServicesForUuidAsync(serviceUuid);
            GattCharacteristicsResult currentCharacteristicResult = await service.Services[0].GetCharacteristicsForUuidAsync(characteristicUuid);
            GattCharacteristic characteristic;

            if (currentCharacteristicResult.Status == GattCommunicationStatus.AccessDenied || currentCharacteristicResult.Status == GattCommunicationStatus.ProtocolError)
            {
                Debug.WriteLine($"Error while getting characteristic: {characteristicUuid.ToString()} - {currentCharacteristicResult.Status}");
                characteristic = null;
            }
            else
                characteristic = currentCharacteristicResult.Characteristics[0];

            return characteristic;
        }

        /// <summary>
        /// Get List of all characteristics from the specified service
        /// </summary>
        /// <param name="serviceUuid">GATT Service UUID</param>
        /// <returns></returns>
        public static async Task<GattCharacteristicsResult> GetAllCharacteristicsFromService(Guid serviceUuid)
        {
            if (bluetoothLEDevice == null)
                throw new Exception("Cannot get characteristic from service: Device is disconnected.");

            var service = await bluetoothLEDevice.GetGattServicesForUuidAsync(serviceUuid);
            return await service.Services[0].GetCharacteristicsAsync();
        }

        /// <summary>
        /// Get one service by his UUID
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static async Task<GattDeviceServicesResult> GetServiceByUuid(Guid serviceUuid)
        {
            if (bluetoothLEDevice == null)
                throw new Exception("Cannot get characteristic from service: Device is disconnected.");

            return await bluetoothLEDevice.GetGattServicesForUuidAsync(serviceUuid);
        }
    }
}