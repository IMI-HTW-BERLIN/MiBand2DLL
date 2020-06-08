namespace Data.ResponseTypes
{
    public class DeviceConnectionResponse
    {
        public bool IsConnected { get; }

        public DeviceConnectionResponse(bool isConnected) => IsConnected = isConnected;
    }
}