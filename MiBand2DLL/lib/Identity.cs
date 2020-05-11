using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        /// <summary>
        /// Check if band reached Auth-Level 1 (tap on the band)
        /// </summary>
        public bool IsAuthenticated { get; private set; }

        private GattCharacteristic _authCharacteristic;
        private readonly EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        

        /// <summary>
        /// Get already paired to device MI Band 2
        /// </summary>
        /// <returns>DeviceInformation with Band data. If device is not paired, returns null</returns>
        public async Task<DeviceInformation> GetPairedBand()
        {
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
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
            _authCharacteristic = await Gatt.GetCharacteristicByServiceUuid(Consts.Guids.AUTH_SERVICE, Consts.Guids.AUTH_CHARACTERISTIC);
            await _authCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            
            // Authenticate on Level-1
            if (!IsAuthenticated)
            {
                List<byte> sendKey = new List<byte> {0x01, 0x08};
                sendKey.AddRange(Consts.Guids.AUTH_SECRET_KEY);

                if (await _authCharacteristic.WriteValueAsync(sendKey.ToArray().AsBuffer()) !=
                    GattCommunicationStatus.Success)
                    return false;
            }
            
            _authCharacteristic.ValueChanged += AuthCharacteristic_ValueChanged;

            _waitHandle.WaitOne();
            return IsAuthenticated;
        }

        /// <summary>
        /// AuthCharacteristic handler. Checking input requests to device from Band
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void AuthCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (sender.Uuid == Consts.Guids.AUTH_CHARACTERISTIC)
            {
                List<byte> request = args.CharacteristicValue.ToArray().ToList();
                byte authResponse = 0x10;
                byte authSendKey = 0x01;
                byte authRequestRandomAuthNumber = 0x02;
                byte authRequestEncryptedKey = 0x03;
                byte authSuccess = 0x01;
                byte authFail = 0x04;

                if (request[2] == authFail)
                {
                    Debug.WriteLine("Authentication error");
                    _waitHandle.Set();
                }

                if (request[0] == authResponse && request[1] == authSendKey && request[2] == authSuccess)
                {
                    Debug.WriteLine("Level 2 started");

                    if (await SendAuthKey())
                        Debug.WriteLine("Level 2 success");
                }
                else if (request[0] == authResponse && request[1] == authRequestRandomAuthNumber && request[2] == authSuccess)
                {
                    Debug.WriteLine("Level 3 started");

                    if (await SendEncryptedRandomKeyAsync(args))
                        Debug.WriteLine("Level 3 success");
                }
                else if (request[0] == authResponse && request[1] == authRequestEncryptedKey && request[2] == authSuccess)
                {
                    Debug.WriteLine("Authentication completed");
                    IsAuthenticated = true;
                    _waitHandle.Set();
                }
            }
        }

        /// <summary>
        /// Encrypt Secret key and last 16 bytes from response in AES/ECB/NoPadding Encryption.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] Encrypt(byte[] data)
        {
            byte[] secretKey = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45 };
            IBuffer key = secretKey.AsBuffer();
            SymmetricKeyAlgorithmProvider algorithmProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);
            CryptographicKey ckey = algorithmProvider.CreateSymmetricKey(key);

            IBuffer buffEncrypt = CryptographicEngine.Encrypt(ckey, data.AsBuffer(), null);
            return buffEncrypt.ToArray();
        }

        /// <summary>
        /// Sending auth number to band (Auth Level 2)
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SendAuthKey()
        {
            Debug.WriteLine("Sending Auth Number");
            List<byte> authNumber = new List<byte>();
            authNumber.Add(0x02);
            authNumber.Add(0x08);

            return await _authCharacteristic.WriteValueAsync(authNumber.ToArray().AsBuffer()) == GattCommunicationStatus.Success;
        }

        /// <summary>
        /// Sending Encrypted random key to band (Auth Level 3)
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<bool> SendEncryptedRandomKeyAsync(GattValueChangedEventArgs args)
        {
            List<byte> randomKey = new List<byte>();
            List<byte> relevantResponsePart = new List<byte>();
            var responseValue = args.CharacteristicValue.ToArray();

            for (int i = 0; i < responseValue.Count(); i++)
            {
                if (i >= 3)
                    relevantResponsePart.Add(responseValue[i]);
            }

            randomKey.Add(0x03);
            randomKey.Add(0x08);
            randomKey.AddRange(Encrypt(relevantResponsePart.ToArray()));

            return await _authCharacteristic.WriteValueAsync(randomKey.ToArray().AsBuffer()) == GattCommunicationStatus.Success;
        }
    }
}
