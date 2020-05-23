using System;

namespace MiBand2DLL.CustomExceptions
{
    [Serializable]
    public class NotInitializedException : Exception
    {
        public NotInitializedException()
        {
        }

        public NotInitializedException(string message) : base(message)
        {
        }
    }
}