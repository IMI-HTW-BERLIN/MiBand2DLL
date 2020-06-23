using System;

namespace MiBand2DLL.lib
{
    /// <summary>
    /// All band related constant data, grouped into different sections.
    /// </summary>
    internal static class Consts
    {
        public static class General
        {
            public const string MI_BAND_NAME = "MI Band 2";
        }

        /// <summary>
        /// UUIDs used for accessing services and characteristics of the band.
        /// <para>
        /// UUIDs can be taken from unofficial MIBand2-Protocol:
        /// https://github.com/aashari/mi-band-2/blob/master/README.md
        /// </para>
        /// </summary>
        public static class Guids
        {
            public static readonly Guid HR_SERVICE = new Guid("0000180d-0000-1000-8000-00805f9b34fb");

            public static readonly Guid HR_MEASUREMENT_CHARACTERISTIC =
                new Guid("00002a37-0000-1000-8000-00805f9b34fb");

            public static readonly Guid HR_CONTROL_POINT_CHARACTERISTIC =
                new Guid("00002a39-0000-1000-8000-00805f9b34fb");

            public static readonly Guid AUTH_SERVICE = new Guid("0000fee1-0000-1000-8000-00805f9b34fb");
            public static readonly Guid AUTH_CHARACTERISTIC = new Guid("00000009-0000-3512-2118-0009af100700");


            public static readonly Guid SENSOR_SERVICE = new Guid("0000fee0-0000-1000-8000-00805f9b34fb");
            public static readonly Guid SENSOR_CHARACTERISTIC = new Guid("00000001-0000-3512-2118-0009af100700");
        }

        /// <summary>
        /// Commands related to the heart rate functionality.
        /// </summary>
        public static class HeartRate
        {
            public static readonly byte[] HR_START_COMMAND = {0x15, 0x01, 0x01};
            public static readonly byte[] HR_STOP_COMMAND = {0x15, 0x01, 0x00};
            public static readonly byte[] HR_CONTINUE_COMMAND = {0x16};
        }

        /// <summary>
        /// Commands and response-codes related to the authentication functionality.
        /// </summary>
        public static class Auth
        {
            public static readonly byte[] AUTH_KEY = {0x01, 0x08};
            public static readonly byte[] AUTH_SECOND_KEY = {0x02, 0x08};

            public static readonly byte[] AUTH_SECRET_KEY =
                {0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45};


            public const byte AUTH_RESPONSE = 0x10;
            public const byte AUTH_KEY_RECEIVED = 0x01;
            public const byte AUTH_SECOND_KEY_RECEIVED = 0x02;
            public const byte AUTH_ENCRYPTED_KEY_RECEIVED = 0x03;
            public const byte AUTH_SUCCESS = 0x01;
            public const byte AUTH_NO_USER_INPUT = 0x02;
            public const byte AUTH_FAIL = 0x04;
        }
    }
}