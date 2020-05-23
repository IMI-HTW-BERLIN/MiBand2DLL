using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using MiBand2DLL.CustomExceptions;

namespace MiBand2DLL.lib
{
    /// TODO: Update/Add comments and summaries.
    /// TODO: Finish clean-up/refactoring.
    /// <summary>
    /// Following code was partially inspired by
    /// https://github.com/AL3X1/Mi-Band-2-SDK, 
    /// https://github.com/aashari/mi-band-2 and 
    /// https://github.com/creotiv/MiBand2
    /// </summary>
    internal class Identity
    {
        public bool Authenticated { get; private set; }

        public bool IsInitialized;

        private GattDeviceService _authService;

        private GattCharacteristic _authCharacteristic;


        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public async Task Initialize(BluetoothLEDevice device)
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

            IsInitialized = true;
        }

        public void Dispose()
        {
            IsInitialized = false;
            Authenticated = false;
            _authService?.Dispose();
            _authCharacteristic = null;
        }

        /// <summary>
        /// Starts the authentication process.
        /// </summary>
        /// <returns></returns>
        public async Task AuthenticateAsync() => await SendUserHandshakeRequest(true);

        /// <summary>
        /// Will wait until the user touches the band.
        /// Uses the first level of authentication for user input. Little hack:P
        /// </summary>
        /// <returns></returns>
        public async Task AskForUserTouch() => await SendUserHandshakeRequest(false);

        /// <summary>
        /// Sends a request to the band that will ask the user to touch the band. Also known as Level-1 Authentication.
        /// Will wait until the user touches the band!
        /// </summary>
        /// <returns></returns>
        private async Task SendUserHandshakeRequest(bool inAuthenticationProcess)
        {
            if (_authCharacteristic == null)
                throw new NotInitializedException(
                    "Heart rate functionality is not yet initialized. " +
                    "Make sure it is initialized before calling any auth-related methods.");

            if (inAuthenticationProcess)
                _authCharacteristic.ValueChanged += ListenForAuthMessage;
            else
                _authCharacteristic.ValueChanged += ListenForAuthMessageOneTime;

            List<byte> authKey = new List<byte>(Consts.Auth.AUTH_KEY);
            authKey.AddRange(Consts.Auth.AUTH_SECRET_KEY);
            await _authCharacteristic.WriteValueAsync(authKey.ToArray().AsBuffer());
            _waitHandle.WaitOne();
        }

        private void ListenForAuthMessageOneTime(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            _authCharacteristic.ValueChanged -= ListenForAuthMessageOneTime;
            byte[] bandMessages = args.CharacteristicValue.ToArray();
            if (bandMessages[2] == Consts.Auth.AUTH_SUCCESS)
                _waitHandle.Set();
        }

        /// <summary>
        /// Checks each authentication-progress-level between band and program. Will be called every "level"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args">Data received from the band</param>
        private async void ListenForAuthMessage(GattCharacteristic sender, GattValueChangedEventArgs args)
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
                    await SendSecondAuthKey();
                    break;
                case Consts.Auth.AUTH_SECOND_KEY_RECEIVED:
                    await SendEncryptedRandomKeyAsync(args);
                    break;
                case Consts.Auth.AUTH_ENCRYPTED_KEY_RECEIVED:
                    _authCharacteristic.ValueChanged -= ListenForAuthMessage;
                    Authenticated = true;
                    _waitHandle.Set();
                    break;
            }
        }

        /// <summary>
        /// Sending second auth number to band (Auth Level 2)
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SendSecondAuthKey() =>
            await _authCharacteristic.WriteValueAsync(Consts.Auth.AUTH_SECOND_KEY.AsBuffer()) ==
            GattCommunicationStatus.Success;

        /// <summary>
        /// Sending Encrypted random key to band (Auth Level 3)
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<bool> SendEncryptedRandomKeyAsync(GattValueChangedEventArgs args)
        {
            List<byte> randomKey = new List<byte>();
            byte[] responseValue = args.CharacteristicValue.ToArray();
            byte[] relevantResponsePart = new byte[0];
            if (responseValue.Length >= 3)
                relevantResponsePart = responseValue.SubArray(3, responseValue.Length - 3);

            randomKey.Add(0x03);
            randomKey.Add(0x08);
            randomKey.AddRange(Encrypt(relevantResponsePart));

            return await _authCharacteristic.WriteValueAsync(randomKey.ToArray().AsBuffer()) ==
                   GattCommunicationStatus.Success;
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