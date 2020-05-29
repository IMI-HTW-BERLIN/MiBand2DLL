namespace Data
{
    public static class Consts
    {
        public static class ServerData
        {
            public const int PORT = 4000;
        }

        public static class ServerResponseToken
        {
            public const char IS_INT_CHAR = '°';
            public const char IS_STRING_CHAR = '^';
        }

        public enum Command
        {
            ConnectBand,
            DisconnectBand,
            AuthenticateBand,
            StartMeasurement,
            StopMeasurement,
            SubscribeToHeartRateChange,
            AskUserForTouch,
            StopServer
        }
    }
}