using System;

namespace MiBand2DLL.CustomExceptions.SoftwareRelatedException
{
    /// <summary>
    /// Will happen when device functionality is being accessed at multiple points.
    /// </summary>
    [Serializable]
    public class AccessDeniedException : Exception
    {
        public AccessDeniedException()
        {
        }

        public AccessDeniedException(string message) : base(message)
        {
        }
    }
}