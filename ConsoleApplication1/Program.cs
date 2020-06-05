
using System.Threading.Tasks;
using MiBand2DLL;

namespace ConsoleApplication1
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            await DoIt();
        }

        private static async Task DoIt()
        {
            await MiBand2.ConnectBandAsync();
            await MiBand2.AuthenticateBandAsync();
            await MiBand2.StartMeasurementAsync();
        }
    }
}