using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using MiBand2DLL.CustomExceptions.HardwareRelatedExceptions;
using MiBand2DLL.CustomExceptions.SoftwareRelatedException;
using MiBand2DLL.util;

namespace MiBand2DLL.lib
{
    /// <summary>
    /// Manages the heart rate measuring-functionality, providing methods for starting hear rate measurements.
    /// </summary>
    internal class HeartRate
    {
        /// <summary>
        /// Last measured heart rate. Can be used to simply get the last measured heart rate.
        /// </summary>
        public int LastHeartRate { get; private set; }


        /// <summary>
        /// Event for changing <see cref="LastHeartRate"/>. Will be invoked whenever a new heart rate is measured.
        /// </summary>
        public static event Delegates.HeartRateDelegate OnHeartRateChange;


        /// <summary>
        /// Heart rate service used for getting characteristics for measurements.
        /// </summary>
        private static GattDeviceService _hrService;

        /// <summary>
        /// Heart rate service used for getting characteristics for enabling continuous measurements.
        /// </summary>
        private static GattDeviceService _sensorService;


        /// <summary>
        /// Heart-Rate-Measurement-Characteristic is used for listening for new heart rate measurements.
        /// </summary>
        private GattCharacteristic _hrMeasurementCharacteristic;

        /// <summary>
        /// Heart-Rate-ControlPoint-Characteristic is used for controlling the heart-rate-functionality of the band.
        /// It enables/disables measurements.
        /// </summary>
        private GattCharacteristic _hrControlPointCharacteristic;

        /// <summary>
        /// Basic Sensor-Characteristic used to start the continuous heart-rate-measurement.
        /// </summary>
        private GattCharacteristic _sensorCharacteristic;

        /// <summary>
        /// Whether we are currently measuring the heart rate continuously.
        /// </summary>
        private bool _measureHeartRateContinuous;

        /// <summary>
        /// Whether th heart rate functionality is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Initializes all characteristics.
        /// </summary>
        /// <exception cref="AccessDeniedException"></exception>
        /// <exception cref="DeviceDisconnectedException"></exception>
        public async Task InitializeAsync(BluetoothLEDevice device)
        {
            _hrService = await Gatt.GetServiceByUuid(device, Consts.Guids.HR_SERVICE);
            _sensorService = await Gatt.GetServiceByUuid(device, Consts.Guids.SENSOR_SERVICE);
            // No service, no device.
            if (_hrService == null || _sensorService == null)
                throw new DeviceDisconnectedException("Couldn't get service, the device seems to be disconnected.");

            _hrMeasurementCharacteristic =
                await Gatt.GetCharacteristicFromUuid(_hrService, Consts.Guids.HR_MEASUREMENT_CHARACTERISTIC);
            _hrControlPointCharacteristic =
                await Gatt.GetCharacteristicFromUuid(_hrService, Consts.Guids.HR_CONTROL_POINT_CHARACTERISTIC);
            _sensorCharacteristic =
                await Gatt.GetCharacteristicFromUuid(_sensorService, Consts.Guids.SENSOR_CHARACTERISTIC);

            // Check if there are services but characteristics can't be accessed.
            if (_hrMeasurementCharacteristic == default || _hrControlPointCharacteristic == default ||
                _sensorCharacteristic == default)
                throw new AccessDeniedException("Couldn't get characteristics. Services may be accessed atm.");

            // Enable notification for heart rate measurements, always needed for receiving measurements
            await _hrMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            _isInitialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _isInitialized = false;
            _hrService?.Dispose();
            _sensorService?.Dispose();

            _hrMeasurementCharacteristic = null;
            _hrControlPointCharacteristic = null;
        }

        /// <summary>
        /// Starts the continuous heart rate measurement by repeatedly requesting new ones.
        /// </summary>
        /// <exception cref="NotInitializedException"></exception>
        public async Task StartContinuousHeartRateMeasurementAsync()
        {
            if (!_isInitialized)
                throw new NotInitializedException(
                    "Heart rate functionality is not yet initialized. " +
                    "Make sure it is initialized before calling any heart rate related methods.");

            // Stop everything
            await StopAllMeasurementsAsync();

            // Start continuous hr measurement
            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_START_CONTINUOUS_COMMAND
                .AsBuffer());

            // Needed to actually start the continuous measurement TODO: Really needed?
            byte[] startCommand = {0x02};
            await _sensorCharacteristic.WriteValueAsync(startCommand.AsBuffer());

            // Listen for new heart rate measurement
            _measureHeartRateContinuous = true;
            _hrMeasurementCharacteristic.ValueChanged += HeartRateReceived;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotInitializedException">Functionality not initialized.</exception>
        public async Task StopAllMeasurementsAsync()
        {
            if (!_isInitialized)
                throw new NotInitializedException(
                    "Can't stop measurements without characteristics being initialized. " +
                    "Make sure it is initialized before calling any heart rate related methods.");

            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_STOP_SINGLE_COMMAND.AsBuffer());
            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_STOP_CONTINUOUS_COMMAND.AsBuffer());
            if (_measureHeartRateContinuous)
            {
                _measureHeartRateContinuous = false;
                _hrMeasurementCharacteristic.ValueChanged -= HeartRateReceived;
            }
        }

        /// <summary>
        /// Start single heart rate measurement. After completion, <see cref="LastHeartRate"/> will be updated with the
        /// new heart rate.
        /// </summary>
        /// <exception cref="NotInitializedException"></exception>
        public async Task StartSingleHeartRateMeasurementAsync()
        {
            if (!_isInitialized)
                throw new NotInitializedException(
                    "Heart rate functionality is not yet initialized. " +
                    "Make sure it is initialized before calling any heart rate related methods.");

            // Stop everything
            await StopAllMeasurementsAsync();

            // Start single hr measurement
            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_START_SINGLE_COMMAND.AsBuffer());

            // Listen for new heart rate measurement
            _measureHeartRateContinuous = false;
            _hrMeasurementCharacteristic.ValueChanged += HeartRateReceived;
        }

        /// <summary>
        /// Handles incoming measurements from the band.
        /// If <see cref="_measureHeartRateContinuous"/> is true, will send a request to get the next measurement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">The received hr-measurement</param>
        private void HeartRateReceived(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            LastHeartRate = args.CharacteristicValue.ToArray()[1];
            OnHeartRateChange?.Invoke(LastHeartRate);
            // TODO: Test how often this is needed
            if (_measureHeartRateContinuous)
                SendNextHRRequest().Wait();
            else
                _hrMeasurementCharacteristic.ValueChanged -= HeartRateReceived;
        }

        /// <summary>
        /// Sends a request to measure the heart rate again.
        /// Used by <see cref="StartContinuousHeartRateMeasurementAsync"/>.
        /// </summary>
        private async Task SendNextHRRequest() =>
            await _hrControlPointCharacteristic.WriteValueAsync(new byte[] {0x16}.AsBuffer());
    }
}