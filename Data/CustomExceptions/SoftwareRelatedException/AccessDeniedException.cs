using System;

namespace Data.CustomExceptions.SoftwareRelatedException
{
    /// <summary>
    /// Will happen when device functionality is being accessed at multiple points.
    /// </summary>
    [Serializable]
    public class AccessDeniedException : Exception, ICustomException
    {
        public AccessDeniedException()
        {
        }

        public AccessDeniedException(string message) : base(message)
        {
        }
    }
}