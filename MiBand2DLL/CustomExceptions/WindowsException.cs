using System;

namespace MiBand2DLL.CustomExceptions
{
    [Serializable]
    public class WindowsException : Exception
    {
        public WindowsException()
        {
        }

        public WindowsException(string message) : base(message)
        {
        }
    }
}