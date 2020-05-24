using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using MiBand2DLL.CustomExceptions.HardwareRelatedExceptions;
using MiBand2DLL.CustomExceptions.SoftwareRelatedException;

namespace MiBand2DLL.lib
{
    /// <summary>
    /// Manages the authentication-functionality, providing methods for authenticating the band.
    /// </summary>
    internal class Authentication
    {
        /// <summary>
        /// Whether the band is authenticated.
        /// </summary>
        public bool Authenticated { get; private set; }

        /// <summary>
        /// Whether the auth functionality is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Authentication service used for accessing the auth functionality of the band.
        /// </summary>
        private GattDeviceService _authService;

        /// <summary>
        /// Auth-Characteristic used to authenticate the band.
        /// </summary>
        private GattCharacteristic _authCharacteristic;

        /// <summary>
        /// Flag to prevent multiple authentication processes.
        /// </summary>
        private bool _isInAuthenticationProcess;

        /// <summary>
        /// Used for waiting for a response from the band.
        /// </summary>
        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        /// <summary>
        /// Initializes the authentication functionality.
        /// </summary>
        /// <param name="device">The currently connected bluetooth device (aka. the band)</param>
        /// <exception cref="DeviceDisconnectedException">Device is disconnected.</exception>
        /// <exception cref="AccessDeniedException">Device can't be accessed due to being accessed by something else.</exception>
        public async Task InitializeAsync(BluetoothLEDevice device)
        {
            if (_authCharacteristic != null)
                Dispose();

            _authService = await Gatt.GetServiceByUuid(device, Consts.Guids.AUTH_SERVICE);
            // No service, no device.
            if (_authService == null)
                throw new DeviceDisconnectedException("Couldn't get service, the device seems to be disconnected.");

            _authCharacteristic =
                await Gatt.GetCharacteristicFromUuid(_authService, Consts.Guids.AUTH_CHARACTERISTIC);
            // Check if there are services but characteristics can't be accessed.
            if (_authCharacteristic == default)
                throw new AccessDeniedException("Couldn't get characteristics. Services may be accessed atm.");
            // Enable notification for user input.
            await _authCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            _isInitialized = true;
        }

        /// <summary>
        /// Dispose of all references. This is needed to disconnect the device.
        /// </summary>
        public void Dispose()
        {
            _isInitialized = false;
            Authenticated = false;
            _authService?.Dispose();
            _authCharacteristic = null;
        }

        /// <summary>
        /// Starts the authentication process.
        /// </summary>
        /// <exception cref="AccessDeniedException">Device can't be accessed due to being accessed by something else.</exception>
        /// <exception cref="NotInitializedException">Auth functionality not initialized.</exception>
        public async Task AuthenticateAsync()
        {
            if (_isInAuthenticationProcess)
                throw new AccessDeniedException("In authentication process. " +
                                                "Can't start another authentication process.");
            _isInAuthenticationProcess = true;
            await SendUserHandshakeRequestAsync(true);
        }

        /// <summary>
        /// Will wait until the user touches the band.
        /// Uses the first level of authentication for user input. Little hack:P
        /// </summary>
        /// <exception cref="NotInitializedException">Auth functionality not initialized.</exception>
        public async Task AskForUserTouchAsync() => await SendUserHandshakeRequestAsync(false);

        /// <summary>
        /// Sends a request to the band that will ask the user to touch the band. Also known as Level-1 Authentication.
        /// Will wait until the user touches the band!
        /// </summary>
        /// <exception cref="NotInitializedException">Auth functionality not initialized.</exception>
        private async Task SendUserHandshakeRequestAsync(bool inAuthenticationProcess)
        {
            if (!_isInitialized)
            {
                _isInAuthenticationProcess = false;
                throw new NotInitializedException(
                    "Auth functionality is not yet initialized. " +
                    "Make sure it is initialized before calling any auth-related methods.");
            }

            if (inAuthenticationProcess)
                _authCharacteristic.ValueChanged += ListenForAuthMessageAsync;
            else
                _authCharacteristic.ValueChanged += ListenForAuthMessageOneTime;

            List<byte> authKey = new List<byte>(Consts.Auth.AUTH_KEY);
            authKey.AddRange(Consts.Auth.AUTH_SECRET_KEY);
            await _authCharacteristic.WriteValueAsync(authKey.ToArray().AsBuffer());
            _waitHandle.WaitOne();
        }

        /// <summary>
        /// Checks the next auth response. Used by <see cref="AskForUserTouchAsync"/>.
        /// </summary>
        private void ListenForAuthMessageOneTime(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            _authCharacteristic.ValueChanged -= ListenForAuthMessageOneTime;
            byte[] bandMessages = args.CharacteristicValue.ToArray();
            if (bandMessages[2] == Consts.Auth.AUTH_SUCCESS)
                _waitHandle.Set();
        }

        /// <summary>
        /// Checks each authentication-progress-level between band and program. Will be called every "level".
        /// </summary>
        /// <param name="sender">Sending characteristic</param>
        /// <param name="args">Data received from the band</param>
        private async void ListenForAuthMessageAsync(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] bandMessages = args.CharacteristicValue.ToArray();
            byte messageType = bandMessages[0];
            byte lastMessageReceived = bandMessages[1];
            byte messageStatus = bandMessages[2];

            // Check if current message is a authentication response and if it was successful
            if (messageType != Consts.Auth.AUTH_RESPONSE || messageStatus == Consts.Auth.AUTH_FAIL)
                _waitHandle.Set();


            switch (lastMessageReceived)
            {
                case Consts.Auth.AUTH_KEY_RECEIVED:
                    await SendSecondAuthKeyAsync();
                    break;
                case Consts.Auth.AUTH_SECOND_KEY_RECEIVED:
                    await SendEncryptedRandomKeyAsync(args);
                    break;
                case Consts.Auth.AUTH_ENCRYPTED_KEY_RECEIVED:
                    _authCharacteristic.ValueChanged -= ListenForAuthMessageAsync;
                    Authenticated = true;
                    _isInAuthenticationProcess = false;
                    _waitHandle.Set();
                    break;
            }
        }

        /// <summary>
        /// Sending second auth number to band (Auth Level 2).
        /// </summary>
        private async Task SendSecondAuthKeyAsync() =>
            await _authCharacteristic.WriteValueAsync(Consts.Auth.AUTH_SECOND_KEY.AsBuffer());

        /// <summary>
        /// Sending Encrypted random key to band (Auth Level 3).
        /// </summary>
        /// <param name="args">Return value from band-response needed for creating the encrypted key</param>
        private async Task SendEncryptedRandomKeyAsync(GattValueChangedEventArgs args)
        {
            List<byte> randomKey = new List<byte>();
            byte[] responseValue = args.CharacteristicValue.ToArray();
            byte[] relevantResponsePart = new byte[0];
            if (responseValue.Length >= 3)
                relevantResponsePart = responseValue.SubArray(3, responseValue.Length - 3);

            randomKey.Add(0x03);
            randomKey.Add(0x08);
            randomKey.AddRange(Encrypt(relevantResponsePart));

            await _authCharacteristic.WriteValueAsync(randomKey.ToArray().AsBuffer());
        }

        /// <summary>
        /// Encrypt Secret key and last 16 bytes from response in AES/ECB/NoPadding Encryption.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] Encrypt(byte[] data)
        {
            IBuffer key = Consts.Auth.AUTH_SECRET_KEY.AsBuffer();
            SymmetricKeyAlgorithmProvider algorithmProvider =
                SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);
            CryptographicKey cKey = algorithmProvider.CreateSymmetricKey(key);

            IBuffer buffEncrypt = CryptographicEngine.Encrypt(cKey, data.AsBuffer(), null);
            return buffEncrypt.ToArray();
        }
    }
}