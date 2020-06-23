namespace Data
{
    /// <summary>
    /// Contains all constant values.
    /// </summary>
    public static class Consts
    {
        /// <summary>
        /// All server-related constant values.
        /// </summary>
        public static class ServerData
        {
            /// <summary>
            /// The port that will be used for the server.
            /// </summary>
            public const int PORT = 4000;
        }

        /// <summary>
        /// The different commands that can be send to the server/band.
        /// </summary>
        public enum Command
        {
            /// <summary>
            /// Connects the band to the computer.
            /// </summary>
            ConnectBand,

            /// <summary>
            /// Disconnects the band from the computer.
            /// </summary>
            DisconnectBand,

            /// <summary>
            /// Authenticates the band, allowing more features.
            /// </summary>
            AuthenticateBand,

            /// <summary>
            /// Starts the heart rate measurement.
            /// </summary>
            StartMeasurement,

            /// <summary>
            /// Stops the heart rate measurement.
            /// </summary>
            StopMeasurement,

            /// <summary>
            /// Will automatically send the heart rate change to the client.
            /// </summary>
            SubscribeToHeartRateChange,

            /// <summary>
            /// Will automatically send the connection status change to the client.
            /// </summary>
            SubscribeToDeviceConnectionStatusChanged,

            /// <summary>
            /// Ask the user to touch the band.
            /// </summary>
            AskUserForTouch,

            /// <summary>
            /// Stops the server and disconnects the band.
            /// </summary>
            StopServer
        }
    }
}