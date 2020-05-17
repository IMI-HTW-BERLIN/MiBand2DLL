using System;
using System.Diagnostics;
using System.Linq;
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
        /// If device is not connected, property will be null.
        /// </summary>
        public static BluetoothLEDevice BluetoothLeDevice { get; set; }

        /// <summary>
        /// Get GATT characteristic by service UUID.
        /// Service UUID can be taken from unofficial MIBand2-Protocol:
        /// https://github.com/aashari/mi-band-2/blob/master/README.md
        /// </summary>
        /// <param name="service">GATT Service</param>
        /// <param name="characteristicUuid">GATT Characteristic UUID</param>
        /// <returns>GattCharacteristic object if characteristic is exists, else returns null</returns>
        public static async Task<GattCharacteristic> GetCharacteristicFromUuid(GattDeviceService service,
            Guid characteristicUuid)
        {
            GattCharacteristicsResult currentResult = await service.GetCharacteristicsAsync();

            if (currentResult.Status != GattCommunicationStatus.Success)
                Debug.WriteLine(
                    $"Error while getting characteristic: {characteristicUuid.ToString()} - {currentResult.Status}");

            return currentResult.Characteristics.First(c =>
                c.Uuid == characteristicUuid);
        }

        /// <summary>
        /// Get GATT characteristic by service UUID.
        /// Service UUID can be taken from unofficial MIBand2-Protocol:
        /// https://github.com/aashari/mi-band-2/blob/master/README.md
        /// </summary>
        /// <param name="serviceUuid">GATT Service Uuid</param>
        /// <param name="characteristicUuid">GATT Characteristic UUID</param>
        /// <returns>GattCharacteristic object if characteristic is exists, else returns null</returns>
        public static async Task<GattCharacteristic> GetCharacteristicFromUuid(Guid serviceUuid,
            Guid characteristicUuid)
        {
            GattDeviceService service = await GetServiceByUuid(serviceUuid);
            return await GetCharacteristicFromUuid(service, characteristicUuid);
        }

        /// <summary>
        /// Get one service by his UUID
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static async Task<GattDeviceService> GetServiceByUuid(Guid serviceUuid)
        {
            if (BluetoothLeDevice == null)
                throw new Exception("Cannot get characteristic from service: Device is disconnected.");

            return (await BluetoothLeDevice.GetGattServicesForUuidAsync(serviceUuid)).Services[0];
        }
    }
}