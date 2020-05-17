using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace MiBand2DLL.lib
{
    /// TODO: Update/Add comments and summaries.
    /// TODO: HANDLE DISCONNECTION. UNSUBSCRIBE ON DISCONNECTION.
    /// <summary>
    /// Some of the following code was taken, refactored and adjusted for our own purposes from:
    /// https://github.com/AL3X1/Mi-Band-2-SDK
    /// </summary>
    public class HeartRate
    {
        /// <summary>
        /// Last measured heart rate. Can be used to simply get the last measured heart rate.
        /// </summary>
        public int LastHeartRate { get; private set; }


        /// <summary>
        /// Event for changing <see cref="LastHeartRate"/>. Will be invoked whenever a new heart rate is measured.
        /// </summary>
        public event HeartRateDelegate OnHeartRateChange;

        public delegate void HeartRateDelegate(int newHeartRate);

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
        /// Whether <see cref="_hrMeasurementCharacteristic"/> and <see cref="_hrControlPointCharacteristic"/> are initialized.
        /// </summary>
        private bool CharacteristicsInitialized =>
            _hrMeasurementCharacteristic != null && _hrControlPointCharacteristic != null &&
            _sensorCharacteristic != null;

        private bool _measureHeartRateContinuous;

        /// <summary>
        /// Starts the continuous heart rate measurement by repeatedly requesting new ones.
        /// </summary>
        public async Task StartContinuousHeartRateMeasurementAsync()
        {
            if (!CharacteristicsInitialized)
                await InitializeCharacteristics();

            // Stop everything
            await StopAllMeasurements();

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

        public async Task StopAllMeasurements()
        {
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
        public async Task StartSingleHeartRateMeasurementAsync()
        {
            if (!CharacteristicsInitialized)
                await InitializeCharacteristics();

            // Stop everything
            await StopAllMeasurements();

            // Start single hr measurement
            await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_START_SINGLE_COMMAND.AsBuffer());

            // Listen for new heart rate measurement
            _measureHeartRateContinuous = false;
            _hrMeasurementCharacteristic.ValueChanged += HeartRateReceived;
        }

        /// <summary>
        /// Handles incoming measurements from the band.
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

        /// <summary>
        /// Initializes all characteristics.
        /// </summary>
        private async Task InitializeCharacteristics()
        {
            GattDeviceService service = await Gatt.GetServiceByUuid(Consts.Guids.HR_SERVICE);

            _hrMeasurementCharacteristic =
                await Gatt.GetCharacteristicFromUuid(service, Consts.Guids.HR_MEASUREMENT_CHARACTERISTIC);
            _hrControlPointCharacteristic =
                await Gatt.GetCharacteristicFromUuid(service, Consts.Guids.HR_CONTROL_POINT_CHARACTERISTIC);
            _sensorCharacteristic =
                await Gatt.GetCharacteristicFromUuid(Consts.Guids.SENSOR_SERVICE, Consts.Guids.SENSOR_CHARACTERISTIC);

            // Enable notification for heart rate measurements
            await _hrMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
        }
    }
}