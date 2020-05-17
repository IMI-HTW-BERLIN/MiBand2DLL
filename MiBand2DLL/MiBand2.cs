using System.Threading.Tasks;
using MiBand2DLL.lib;

// REVIEW: Do we need MonoBehaviour in here?
// using UnityEngine;

namespace MiBand2DLL
{
    public static class MiBand2
    {
        private static readonly MiBand2SDK Band = new MiBand2SDK();

        /// <summary>
        /// Connects to the band and authenticates the user.
        /// </summary>
        /// <returns>Whether the connection and authentication was successful.</returns>
        public static async Task<bool> ConnectToBand()
        {
            if (!await Band.ConnectAsync())
                return false;

            return await Band.AuthenticateAsync();
        }

        /// <summary>
        /// Measures the heart rate ONCE.
        /// </summary>
        /// <returns>The measured heart rate.</returns>
        public static async Task<int> GetSingleHeartRate()
        {
            await Band.HeartRate.StartSingleHeartRateMeasurementAsync();
            return Band.HeartRate.LastHeartRate;
        }

        public static async Task StartHeartRateMeasureContinuous() =>
            await Band.HeartRate.StartContinuousHeartRateMeasurementAsync();

        public static void SubscribeToHeartRateChange(HeartRate.HeartRateDelegate method) =>
            Band.HeartRate.OnHeartRateChange += method;

        public static async Task StopAllMeasurements() => await Band.HeartRate.StopAllMeasurements();

        public static async Task AskUserForTouch() => await Band.Identity.SendUserHandshakeRequest(false);

        public static async Task<bool> CheckConnection()
        {
            return false;
        }
    }
}