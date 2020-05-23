namespace MiBand2DLL.util
{
    public static class Delegates
    {
        public delegate void HeartRateDelegate(int newHeartRate);

        public delegate void ConnectionStatusDelegate(bool isConnected);
    }
}