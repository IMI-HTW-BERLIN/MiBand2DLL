using System;
using System.Runtime.Serialization;

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

        protected NotAuthenticatedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}