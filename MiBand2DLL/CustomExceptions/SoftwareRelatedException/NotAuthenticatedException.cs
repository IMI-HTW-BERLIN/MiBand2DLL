using System;

namespace MiBand2DLL.CustomExceptions.SoftwareRelatedException
{
    [Serializable]
    public class NotAuthenticatedException : Exception
    {
        public NotAuthenticatedException()
        {
        }

        public NotAuthenticatedException(string message) : base(message)
        {
        }
    }
}