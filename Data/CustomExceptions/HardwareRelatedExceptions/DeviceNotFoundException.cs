using System;
using System.Runtime.Serialization;

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

        protected DeviceNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}