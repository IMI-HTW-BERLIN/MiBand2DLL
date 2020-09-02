using System;

namespace Data.ResponseTypes
{
    /// <summary>
    /// A response that simply indicates that the server received a command and successfully executed it.
    /// </summary>
    [Serializable]
    public struct SuccessResponse
    {
        /// <summary>
        /// The device index of the device that the server communicated with and executed the command with.
        /// </summary>
        public int DeviceIndex { get; private set; }

        /// <summary>
        /// Creates an EmptySuccessResponse
        /// </summary>
        /// <param name="deviceIndex"></param>
        public SuccessResponse(int deviceIndex) => DeviceIndex = deviceIndex;
    }
}