using System.Threading.Tasks;
using MiBand2DLL.lib;
// REVIEW: Do we need MonoBehaviour in here?
// using UnityEngine;

namespace MiBand2DLL
{
    
    
    public static class MiBand2
    {
        private static readonly MiBand2SDk Band = new MiBand2SDk();
        
        /// <summary>
        /// Connects to the band and authenticates the user.
        /// </summary>
        /// <returns>Whether the connection and authentication was successful.</returns>
        public static async Task<bool> ConnectToBand()
        {
            if (!await Band.ConnectAsync()) 
                return false;

            if (!await Band.Identity.AuthenticateAsync())
                return false;

            return true;
        }

        /// <summary>
        /// Measures the heart rate ONCE.
        /// </summary>
        /// <returns>The current heart rate.</returns>
        public static async Task<int> GetHeartRateAsync() => await Band.HeartRate.GetHeartRateAsync();
    }
}