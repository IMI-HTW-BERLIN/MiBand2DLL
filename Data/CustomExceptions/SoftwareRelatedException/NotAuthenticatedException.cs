using System;

namespace Data.CustomExceptions.SoftwareRelatedException
{
    [Serializable]
    public class NotAuthenticatedException : Exception, ICustomException
    {
        public NotAuthenticatedException()
        {
        }

        public NotAuthenticatedException(string message) : base(message)
        {
        }
    }
}