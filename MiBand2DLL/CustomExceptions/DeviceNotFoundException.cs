using System;

namespace MiBand2DLL.CustomExceptions
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