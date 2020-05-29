using System;

namespace Data.CustomExceptions.HardwareRelatedExceptions
{
    [Serializable]
    public class DeviceDisconnectedException : Exception, ICustomException
    {
        public DeviceDisconnectedException()
        {
        }

        public DeviceDisconnectedException(string message) : base(message)
        {
        }
    }
}