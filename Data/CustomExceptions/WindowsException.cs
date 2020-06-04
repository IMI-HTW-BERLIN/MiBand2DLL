using System;
using System.Runtime.Serialization;

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

        protected WindowsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}