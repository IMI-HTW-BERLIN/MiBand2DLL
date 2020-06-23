using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Data.CustomExceptions.HardwareRelatedExceptions;
using Data.CustomExceptions.SoftwareRelatedException;
using Data.ResponseTypes;

namespace MiBand2DLL.lib
{
    /// <summary>
    /// Manages the heart rate measuring-functionality, providing methods for starting hear rate measurements.
    /// </summary>
    internal class HeartRate
    {
        #region Variables

        #region Public

        /// <summary>
        /// Will be invoked whenever a new heart rate is measured.
        /// </summary>
        public static event Action<HeartRateResponse> OnHeartRateChange;

        #endregion

        #region Private

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
        /// Whether th heart rate functionality is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Whether we are currently waiting for the waitTask. Used for limiting the requests to one every 12 seconds.
        /// </summary>
        private bool _sendingNextHRRequest;

        /// <summary>
        /// The time when the last measurement got returned from the band. Used for checking if the heart rate gets
        /// repeated (don't ask me why the band does this by default).
        /// </summary>
        private long _lastTimeMeasureReceived;

        /// <summary>
        /// The last heart rate that got received. Used for checking if the heart rate gets repeated.
        /// </summary>
        private int _lastHeartRate;

        #endregion

        #endregion

        #region Methods

        #region Public

        /// <summary>
        /// Dispose of all references. This is needed to disconnect the device.
        /// </summary>
        public void Dispose()
        {
            _isInitialized = false;
            _hrService?.Dispose();
            _sensorService?.Dispose();

            _hrMeasurementCharacteristic = null;
            _hrControlPointCharacteristic = null;

            OnHeartRateChange = null;
        }

        /// <summary>
        /// Starts the continuous heart rate measurement by repeatedly requesting new ones.
        /// </summary>
        /// <exception cref="DeviceDisconnectedException">Device is currently disconnected.</exception>
        /// <exception cref="AccessDeniedException">Service is already in use.</exception>
        public async Task StartHeartRateMeasurementAsync()
        {
            if (!_isInitialized)
                await InitializeAsync();

            // Stop everything
            await StopMeasurementAsync();

            // Start continuous hr measurement
            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_START_COMMAND
                .AsBuffer());

            // Needed to actually start the continuous measurement.
            byte[] startCommand = {0x02};
            await _sensorCharacteristic.WriteValueAsync(startCommand.AsBuffer());

            // Listen for new heart rate measurement
            _hrMeasurementCharacteristic.ValueChanged += HeartRateReceived;
        }

        /// <summary>
        /// Stops measurement and un-subscribes from Change-Event.
        /// </summary>
        /// <exception cref="DeviceDisconnectedException">Device is currently disconnected.</exception>
        /// <exception cref="AccessDeniedException">Service is already in use.</exception>
        public async Task StopMeasurementAsync()
        {
            if (!_isInitialized)
                await InitializeAsync();

            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_STOP_COMMAND.AsBuffer());
            _hrMeasurementCharacteristic.ValueChanged -= HeartRateReceived;
        }

        #endregion

        #region Private

        /// <summary>
        /// Initializes all characteristics.
        /// </summary>
        /// <exception cref="DeviceDisconnectedException">Device is currently disconnected.</exception>
        /// <exception cref="AccessDeniedException">Service is already in use.</exception>
        private async Task InitializeAsync()
        {
            BluetoothLEDevice device = MiBand2.ConnectedBtDevice;
            if (device == null)
                throw new DeviceDisconnectedException("There is no device connected.");
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
        /// Handles incoming measurements from the band.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">The received hr-measurement</param>
        private async void HeartRateReceived(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            int heartRate = args.CharacteristicValue.ToArray()[1];
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            bool isRepeating = false;
            long measuringTime = currentTime - _lastTimeMeasureReceived;
            if (_lastTimeMeasureReceived != 0 && measuringTime > 4500)
            {
                if (_lastHeartRate == heartRate)
                    isRepeating = true;
                _lastHeartRate = heartRate;
            }
            else
                _lastHeartRate = 0;

            _lastTimeMeasureReceived = currentTime;


            OnHeartRateChange?.Invoke(new HeartRateResponse(heartRate, isRepeating, measuringTime));
            await SendNextHRRequest();
        }

        /// <summary>
        /// Sends a request to measure the heart rate again.
        /// Used by <see cref="HeartRateReceived"/>.
        /// </summary>
        private async Task SendNextHRRequest()
        {
            if (_sendingNextHRRequest)
                return;

            _sendingNextHRRequest = true;
            await Task.Delay(12000);
            _sendingNextHRRequest = false;
            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_CONTINUE_COMMAND.AsBuffer());
        }

        #endregion

        #endregion
    }
}