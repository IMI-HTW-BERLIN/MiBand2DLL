using System;

namespace MiBand2DLL.CustomExceptions
{
    [Serializable]
    public class DeviceDisconnectedException : Exception
    {
        public DeviceDisconnectedException()
        {
        }

        public DeviceDisconnectedException(string message) : base(message)
        {
        }
    }
}