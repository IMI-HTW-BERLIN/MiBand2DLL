using System;
using System.Runtime.Serialization;

namespace Data.CustomExceptions.SoftwareRelatedException
{
    [Serializable]
    public class AuthenticationException : Exception, ICustomException
    {
        public AuthenticationException()
        {
        }

        public AuthenticationException(string message) : base(message)
        {
        }

        protected AuthenticationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}