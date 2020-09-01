namespace Data.ResponseTypes
{
    /// <summary>
    /// A small class used to keep track of the connection status of a device.
    /// </summary>
    public class DeviceConnectionResponse
    {
        /// <summary>
        /// The device index of the device that this connection response is related to.
        /// </summary>
        public int DeviceIndex { get; }

        /// <summary>
        /// Whether the device is connected or not.
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Creates a DeviceConnectionResponse object with the given deviceIndex and connection status.
        /// </summary>
        public DeviceConnectionResponse(int deviceIndex, bool isConnected)
        {
            DeviceIndex = deviceIndex;
            IsConnected = isConnected;
        }
    }
}