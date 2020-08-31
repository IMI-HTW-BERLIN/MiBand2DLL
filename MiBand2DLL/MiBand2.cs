using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Data.CustomExceptions;
using Data.CustomExceptions.HardwareRelatedExceptions;
using Data.CustomExceptions.SoftwareRelatedException;
using Data.ResponseTypes;
using MiBand2DLL.lib;

namespace MiBand2DLL
{
    public class MiBand2
    {
        #region Variables

        #region Public

        /// <summary>
        /// The connected MiBand2 reference. Null if no band is connected.
        /// </summary>
        public BluetoothLEDevice ConnectedBtDevice => _connectedBtDevice;

        /// <summary>
        /// Whether the band is currently connected.
        /// </summary>
        public bool Connected =>
            _connectedBtDevice != null &&
            _connectedBtDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;

        /// <summary>
        /// Whether the band is currently authenticated.
        /// </summary>
        public bool Authenticated => _authentication.Authenticated;

        /// <summary>
        /// Event for connection status changes. This event will fire when the band gets connected and disconnected.
        /// Will automatically reset all references on disconnect.
        /// <para>
        /// </para>
        /// </summary>
        public event ConnectionStatusDelegate DeviceConnectionChanged;

        /// <summary>
        /// Delegate used as a middleman to allow subscription from outside.
        /// </summary>
        /// <param name="deviceIndex">The index of the corresponding device.</param>
        /// <param name="isConnected">Whether the device is connected or disconnected.</param>
        public delegate void ConnectionStatusDelegate(int deviceIndex, bool isConnected);

        #endregion

        #region Private

        /// <summary>
        /// The device index of the physical MiBand2 that this object represents.
        /// </summary>
        private int _deviceIndex;

        /// <summary>
        /// Heart rate functionality used to measure the heart rate.
        /// </summary>
        private readonly HeartRate _heartRate;

        /// <summary>
        /// Authentication functionality used for connecting and authenticating the band with the pc.
        /// </summary>
        private readonly Authentication _authentication;

        /// <summary>
        /// The connected Mi Band 2 reference. Null if no band is connected.
        /// </summary>
        private BluetoothLEDevice _connectedBtDevice;

        /// <summary>
        /// Flag for preventing multiple connection processes.
        /// </summary>
        private static bool _isInConnectionProcess;

        #endregion

        #endregion

        #region Methods

        #region Public

        public MiBand2(int deviceIndex)
        {
            _deviceIndex = deviceIndex;
            _heartRate = new HeartRate(this);
            _authentication = new Authentication(this);
        }

        /// <summary>
        /// Connects to the already paired device.
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="DeviceNotFoundException">Device with given name not found. Is it Paired?</exception>
        /// <exception cref="WindowsException"><see cref="BluetoothLEDevice.FromIdAsync"/> couldn't get device. Debugging required.</exception>
        public async Task ConnectBandAsync(string name = Consts.General.MI_BAND_NAME)
        {
            if (_isInConnectionProcess)
                throw new AccessDeniedException("In connection process. Can't access band atm.");

            _isInConnectionProcess = true;
            DeviceInformation device = await FindDeviceAsync(_deviceIndex, name);
            _connectedBtDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
            _isInConnectionProcess = false;

            if (_connectedBtDevice == null)
                throw new WindowsException("Couldn't get BluetoothLEDevice from DeviceInformation. " +
                                           "Debugging required.");

            _connectedBtDevice.ConnectionStatusChanged += ConnectionStatusChanged;
        }

        /// <summary>
        /// Disconnects the band by disposing all references. Band will stay paired though.
        /// </summary>
        /// <param name="triggerEvent">Should the <see cref="DeviceConnectionChanged"/> event be triggered?</param>
        public void DisconnectBand(bool triggerEvent = true)
        {
            if (triggerEvent)
                DeviceConnectionChanged?.Invoke(_deviceIndex, false);

            DeviceConnectionChanged = null;
            _heartRate.Dispose();
            _authentication.Dispose();
            if (_connectedBtDevice != null)
            {
                _connectedBtDevice.ConnectionStatusChanged -= ConnectionStatusChanged;
                _connectedBtDevice.Dispose();
                _connectedBtDevice = null;
            }

            // Needed to force an immediate update of the connection-status by deleting the removed references.
            GC.Collect();
        }

        /// <summary>
        /// Authenticates the band.
        /// </summary>
        /// <exception cref="AccessDeniedException">Device can't be accessed due to being accessed by something else.</exception>
        public async Task AuthenticateBandAsync() => await _authentication.AuthenticateAsync();

        /// <summary>
        /// Starts the continuous heart rate measurement.
        /// </summary>
        /// <exception cref="DeviceDisconnectedException">Device is disconnected.</exception>
        /// <exception cref="NotAuthenticatedException">Device is not authenticated.</exception>
        public async Task StartMeasurementAsync()
        {
            if (IsConnectedAndAuthenticated())
                await _heartRate.StartHeartRateMeasurementAsync();
        }

        /// <summary>
        /// Stops all measurements immediately.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DeviceDisconnectedException">Device is disconnected.</exception>
        /// <exception cref="NotAuthenticatedException">Device is not authenticated.</exception>
        public async Task StopMeasurementAsync()
        {
            if (IsConnectedAndAuthenticated())
                await _heartRate.StopMeasurementAsync();
        }

        /// <summary>
        /// Subscribes the given method to the "OnHeartRateChange" event. This event will be fired when continuous
        /// measurement is enabled and a new heart rate is received.
        /// </summary>
        /// <param name="method">Method to be subscribed to the OnHeartRateChange event</param>
        public void SubscribeToHeartRateChange(Action<HeartRateResponse> method) =>
            HeartRate.OnHeartRateChange += method;

        /// <summary>
        /// Asks the user to touch the the band.
        /// CAUTION: Can only be cancelled by disconnecting the band.
        /// </summary>
        /// <exception cref="DeviceDisconnectedException">Device is disconnected.</exception>
        public async Task AskUserForTouchAsync()
        {
            if (!Connected)
                throw new DeviceDisconnectedException("The device is currently not connected.");

            await _authentication.AskForUserTouchAsync();
        }

        #endregion

        #region Private

        /// <summary>
        /// Finds the first paired bluetooth-device with the given name.
        /// </summary>
        /// <param name="deviceIndex">The device index of the device.</param>
        /// <param name="name">The name of the device to be searched for.</param>
        /// <returns>The <see cref="DeviceInformation"/> of the device, if found.</returns>
        /// <exception cref="DeviceNotFoundException">Device with given name not found.</exception>
        private async Task<DeviceInformation> FindDeviceAsync(int deviceIndex, string name)
        {
            DeviceInformationCollection devices =
                await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());

            DeviceInformation device = devices.Where(information => information.Name == name).ElementAt(deviceIndex);
            if (device == default)
                throw new DeviceNotFoundException("Couldn't find device. Is it paired?");

            return device;
        }

        /// <summary>
        /// Will be called when <see cref="BluetoothLEDevice.ConnectionStatusChanged"/> is called.
        /// Used as a middleman to allow subscription from outside using <see cref="DeviceConnectionChanged"/>.
        /// </summary>
        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (!Connected)
                DisconnectBand();
            DeviceConnectionChanged?.Invoke(_deviceIndex, Connected);
        }

        /// <summary>
        /// Checks if the band is connected and authenticated.
        /// </summary>
        /// <returns>True if the band is connected and authenticated.</returns>
        /// <exception cref="DeviceDisconnectedException">Device is disconnected.</exception>
        /// <exception cref="NotAuthenticatedException">Device is not authenticated.</exception>
        private bool IsConnectedAndAuthenticated()
        {
            if (!Connected)
                throw new DeviceDisconnectedException("The device is currently not connected.");

            if (!Authenticated)
                throw new NotAuthenticatedException("The device is currently not authenticated. " +
                                                    "Make sure to authenticate the band before using any functionality");
            return true;
        }

        #endregion

        #endregion
    }
}