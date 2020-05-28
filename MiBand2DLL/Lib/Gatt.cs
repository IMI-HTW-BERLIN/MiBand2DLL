using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace MiBand2DLL.lib
{
    /// <summary>
    /// Small class that allows to retrieve services and characteristics from the band using the Gatt-Protocol.
    /// </summary>
    internal static class Gatt
    {
        /// <summary>
        /// Get GATT characteristic by service UUID.
        /// Returns <see cref="GattCharacteristic"/>'s default in case of "Access denied" or "Service not found".
        /// </summary>
        /// <param name="service">GATT Service</param>
        /// <param name="characteristicUuid">GATT Characteristic UUID</param>
        /// <returns>GattCharacteristic object if characteristic is exists, else returns null</returns>
        public static async Task<GattCharacteristic> GetCharacteristicFromUuid(GattDeviceService service,
            Guid characteristicUuid)
        {
            GattCharacteristicsResult currentResult = await service.GetCharacteristicsAsync();
            return currentResult.Characteristics.FirstOrDefault(characteristic =>
                characteristic.Uuid == characteristicUuid);
        }

        /// <summary>
        /// Get one service by his UUID. Returns null if there is no connected devices.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static async Task<GattDeviceService> GetServiceByUuid(BluetoothLEDevice device, Guid serviceUuid)
        {
            return (await device.GetGattServicesForUuidAsync(serviceUuid)).Services[0];
        }
    }
}