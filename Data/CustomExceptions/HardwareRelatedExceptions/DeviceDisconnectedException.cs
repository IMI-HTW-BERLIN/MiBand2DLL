using System;
using System.Runtime.Serialization;

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

        protected DeviceDisconnectedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}