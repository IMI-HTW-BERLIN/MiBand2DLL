using System;

namespace Data.CustomExceptions.HardwareRelatedExceptions
{
    [Serializable]
    public class DeviceNotFoundException : Exception, ICustomException
    {
        public DeviceNotFoundException()
        {
        }

        public DeviceNotFoundException(string message) : base(message)
        {
        }
    }
}