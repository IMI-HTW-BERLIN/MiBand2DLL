namespace MiBand2DLL.util
{
    /// <summary>
    /// Bunch of different statuses for checking the response of the band.
    /// </summary>
    public enum DeviceCommunicationStatus
    {
        Success, Failure, Disconnected, DeviceNotFound, AccessDenied, FunctionalityNotInitialized, NotAuthenticated
    }
}