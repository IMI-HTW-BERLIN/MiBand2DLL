using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace MiBand2DLL.lib
{
    /// TODO: Update/Add comments and summaries.
    /// <summary>
    /// Some of the following code was taken, refactored and adjusted for our own purposes from:
    /// https://github.com/AL3X1/Mi-Band-2-SDK
    /// </summary>
    public class HeartRate
    {
        public enum HRMeasurementType { Single = 0, Continuous = 1 }

        public int LastHeartRate { get; private set; }


        private EventWaitHandle _WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private GattCharacteristic _hrMeasurementCharacteristic;
        private GattCharacteristic _hrControlPointCharacteristic;

        private bool CharacteristicsInitialized =>
            _hrMeasurementCharacteristic != null && _hrControlPointCharacteristic != null;

        /// <summary>
        /// Subscribe to HeartRate notifications from band.
        /// </summary>
        public async Task SubscribeToHeartRateNotificationsAsync()
        {
            _hrMeasurementCharacteristic = await Gatt.GetCharacteristicByServiceUuid(Consts.Guids.HR_SERVICE,
                Consts.Guids.HR_MEASUREMENT_CHARACTERISTIC);
            if (await _hrMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify) == GattCommunicationStatus.Success)
                _hrMeasurementCharacteristic.ValueChanged += HrMeasurementCharacteristicValueChanged;
        }

        /// <summary>
        /// Subscribe to HeartRate notifications from band.
        /// </summary>
        /// <param name="eventHandler">Handler for interact with heartRate values</param>
        /// <returns></returns>
        public async Task SubscribeToHeartRateNotificationsAsync(
            TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> eventHandler)
        {
            _hrMeasurementCharacteristic =
                await Gatt.GetCharacteristicByServiceUuid(Consts.Guids.HR_SERVICE,
                    Consts.Guids.HR_MEASUREMENT_CHARACTERISTIC);

            if (await _hrMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify) == GattCommunicationStatus.Success)
                _hrMeasurementCharacteristic.ValueChanged += eventHandler;
        }

        /// <summary>
        /// Measure current heart rate
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetHeartRateAsync()
        {
            int heartRate = 0;
            if (await StartHeartRateMeasurementAsync() == GattCommunicationStatus.Success)
                heartRate = LastHeartRate;

            return heartRate;
        }

        private async Task InitializeCharacteristics()
        {
            GattCharacteristicsResult characteristics =
                await Gatt.GetAllCharacteristicsFromService(Consts.Guids.HR_SERVICE);
            foreach (GattCharacteristic gattCharacteristic in characteristics.Characteristics)
            {
                if (gattCharacteristic.Uuid == Consts.Guids.HR_MEASUREMENT_CHARACTERISTIC)
                    _hrMeasurementCharacteristic = gattCharacteristic;
                else if (gattCharacteristic.Uuid == Consts.Guids.HR_CONTROL_POINT_CHARACTERISTIC)
                    _hrControlPointCharacteristic = gattCharacteristic;
            }
        }

        /// <summary>
        /// Starting heart rate measurement
        /// </summary>
        /// <returns></returns>
        private async Task<GattCommunicationStatus> StartHeartRateMeasurementAsync()
        {
            if (!CharacteristicsInitialized)
                await InitializeCharacteristics();

            GattCommunicationStatus status = GattCommunicationStatus.ProtocolError;

            GattCommunicationStatus gattStatus =
                await _hrMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);

            if (gattStatus != GattCommunicationStatus.Success)
                return status;

            gattStatus =
                await _hrControlPointCharacteristic.WriteValueAsync(Consts.HeartRate.HR_START_COMMAND.AsBuffer());

            if (gattStatus != GattCommunicationStatus.Success)
                return status;

            _hrMeasurementCharacteristic.ValueChanged += HrMeasurementCharacteristicValueChanged;
            status = GattCommunicationStatus.Success;
            _WaitHandle.WaitOne();

            return status;
        }

        /// <summary>
        /// Handle incoming requests with heart rate from the band.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HrMeasurementCharacteristicValueChanged(GattCharacteristic sender,
            GattValueChangedEventArgs args)
        {
            if (sender.Uuid.ToString() == Consts.Guids.HR_MEASUREMENT_CHARACTERISTIC.ToString())
                LastHeartRate = args.CharacteristicValue.ToArray()[1];
            _WaitHandle.Set();
        }

        /// <summary>
        /// Set continuous heart rate measurements
        /// </summary>
        /// <param name="measurementsType"></param>
        /// <returns></returns>
        public async Task<GattCommunicationStatus> SetContinuousHeartRateMeasurement(
            HRMeasurementType measurementsType)
        {
            if (!CharacteristicsInitialized)
                await InitializeCharacteristics();

            GattCommunicationStatus status = GattCommunicationStatus.ProtocolError;

            GattCommunicationStatus gattStatus =
                await _hrMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);

            if (gattStatus != GattCommunicationStatus.Success)
                return status;

            byte[] manualCmd;
            byte[] continuousCmd;

            if (measurementsType == HRMeasurementType.Continuous)
            {
                manualCmd = new byte[] {0x15, 0x02, 0};
                continuousCmd = new byte[] {0x15, 0x01, 1};
            }
            else
            {
                manualCmd = new byte[] {0x15, 0x02, 1};
                continuousCmd = new byte[] {0x15, 0x01, 0};
            }

            if (await _hrControlPointCharacteristic.WriteValueAsync(manualCmd.AsBuffer()) !=
                GattCommunicationStatus.Success ||
                await _hrControlPointCharacteristic.WriteValueAsync(continuousCmd.AsBuffer()) !=
                GattCommunicationStatus.Success)
                return status;

            status = GattCommunicationStatus.Success;
            _hrMeasurementCharacteristic.ValueChanged += HrMeasurementCharacteristicValueChanged;

            return status;
        }

        /// <summary>
        /// Sets Heart Rate Measurement interval in minutes (0 is off)
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public async Task<bool> SetHeartRateMeasurementInterval(int minutes)
        {
            _hrControlPointCharacteristic =
                await Gatt.GetCharacteristicByServiceUuid(Consts.Guids.HR_SERVICE,
                    Consts.Guids.HR_CONTROL_POINT_CHARACTERISTIC);
            return await _hrControlPointCharacteristic.WriteValueAsync(
                new byte[] {0x14, (byte) minutes}.AsBuffer()) == GattCommunicationStatus.Success;
        }
    }
}