using System;

namespace MiBand2DLL.CustomExceptions.HardwareRelatedExceptions
{
    [Serializable]
    public class DeviceNotFoundException : Exception
    {
        public DeviceNotFoundException()
        {
        }

        public DeviceNotFoundException(string message) : base(message)
        {
        }
    }
}