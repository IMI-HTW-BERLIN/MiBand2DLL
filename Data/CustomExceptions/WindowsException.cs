using System;

namespace Data.CustomExceptions
{
    [Serializable]
    public class WindowsException : Exception, ICustomException
    {
        public WindowsException()
        {
        }

        public WindowsException(string message) : base(message)
        {
        }
    }
}