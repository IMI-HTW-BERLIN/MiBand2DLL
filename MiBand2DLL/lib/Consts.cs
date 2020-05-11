using System;

namespace MiBand2DLL.lib
{
    public static class Consts
    {
        /// Service UUID can be taken from unofficial MIBand2-Protocol:
        /// https://github.com/aashari/mi-band-2/blob/master/README.md
        public static class Guids
        {
            public static readonly Guid HR_SERVICE = new Guid("0000180d-0000-1000-8000-00805f9b34fb");

            public static readonly Guid HR_MEASUREMENT_CHARACTERISTIC =
                new Guid("00002a37-0000-1000-8000-00805f9b34fb");

            public static readonly Guid HR_CONTROL_POINT_CHARACTERISTIC =
                new Guid("00002a39-0000-1000-8000-00805f9b34fb");

            public static readonly Guid AUTH_SERVICE = new Guid("0000FEE1-0000-1000-8000-00805F9B34FB");
            public static readonly Guid AUTH_CHARACTERISTIC = new Guid("00000009-0000-3512-2118-0009af100700");

            public static readonly byte[] AUTH_SECRET_KEY =
                {0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45};
        }

        public static class HeartRate
        {
            public static readonly byte[] HR_START_COMMAND = {21, 2, 1};
        }
    }
}