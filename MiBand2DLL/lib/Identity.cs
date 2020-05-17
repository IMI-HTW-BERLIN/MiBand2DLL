using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace MiBand2DLL.lib
{
    /// TODO: Update/Add comments and summaries.
    /// TODO: Finish clean-up/refactoring.
    /// <summary>
    /// Some of the following code was taken, refactored and adjusted for our own purposes from:
    /// https://github.com/AL3X1/Mi-Band-2-SDK
    /// </summary>
    public class Identity
    {
        private GattCharacteristic _authCharacteristic;
        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool lastAuthenticationSuccessfull;

        /// <summary>
        /// Get already paired to device MI Band 2
        /// </summary>
        /// <returns>DeviceInformation with Band data. If device is not paired, returns null</returns>
        public async Task<DeviceInformation> GetPairedBand()
        {
            DeviceInformationCollection devices =
                await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
            DeviceInformation deviceInfo = null;

            foreach (DeviceInformation device in devices)
            {
                //if (device.Pairing.IsPaired && device.Name == "MI Band 2") 
                //    deviceInfo = device;
                // HACK: Device is paired even though this returns false, ignore for now and trust that the band is connected :D
                // TODO: Look into "wrong" status of band-pairing.
                if (device.Name == "MI Band 2")
                    deviceInfo = device;
            }

            return deviceInfo;
        }

        /// <summary>
        /// Authentication. If already authenticated, just send AuthNumber and EncryptedKey to band (auth levels 2 and 3)
        /// </summary>
        /// <returns></returns>
        public async Task<bool> AuthenticateAsync()
        {
            if (_authCharacteristic == null)
            {
                _authCharacteristic =
                    await Gatt.GetCharacteristicFromUuid(Consts.Guids.AUTH_SERVICE, Consts.Guids.AUTH_CHARACTERISTIC);
                await _authCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }


            // Authenticate on Level-1 (Tap on band)
            List<byte> authKey = new List<byte>(Consts.Auth.AUTH_KEY);
            authKey.AddRange(Consts.Auth.AUTH_SECRET_KEY);

            if (!await SendUserHandshakeRequest(true))
                return false;

            lastAuthenticationSuccessfull = false;
            return true;
        }

        /// <summary>
        /// Sends a request to the band that will ask the user to touch the band. Also known as Level-1 Authentication.
        /// can be used to
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SendUserHandshakeRequest(bool inAuthenticationProcess)
        {
            List<byte> authKey = new List<byte>(Consts.Auth.AUTH_KEY);
            authKey.AddRange(Consts.Auth.AUTH_SECRET_KEY);
            if (inAuthenticationProcess)
                _authCharacteristic.ValueChanged += ListenForAuthMessage;
            else
                _authCharacteristic.ValueChanged += ListenForAuthMessageOneTime;


            GattCommunicationStatus status = await _authCharacteristic.WriteValueAsync(authKey.ToArray().AsBuffer());
            _waitHandle.WaitOne();
            return status == GattCommunicationStatus.Success;
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
                    if (!await SendSecondAuthKey())
                        _waitHandle.Set();
                    break;
                case Consts.Auth.AUTH_SECOND_KEY_RECEIVED:
                    if (!await SendEncryptedRandomKeyAsync(args))
                        _waitHandle.Set();
                    break;
                case Consts.Auth.AUTH_ENCRYPTED_KEY_RECEIVED:
                    lastAuthenticationSuccessfull = true;
                    _authCharacteristic.ValueChanged -= ListenForAuthMessage;
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
        private byte[] Encrypt(byte[] data)
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