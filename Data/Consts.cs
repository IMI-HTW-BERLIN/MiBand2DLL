namespace Data
{
    public static class Consts
    {
        public static class ServerData
        {
            public const int PORT = 4000;
        }

        public enum Command
        {
            ConnectBand,
            DisconnectBand,
            AuthenticateBand,
            StartMeasurement,
            StopMeasurement,

            /// <summary>
            /// Will automatically send the heart rate change to the client whenever it changes.
            /// </summary>
            SubscribeToHeartRateChange,
            SubscribeToDeviceConnectionStatusChanged,
            AskUserForTouch,
            StopServer
        }
    }
}