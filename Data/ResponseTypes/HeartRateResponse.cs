using System;

namespace Data.ResponseTypes
{
    [Serializable]
    public class HeartRateResponse
    {
        public int HeartRate { get; private set; }

        public HeartRateResponse(int heartRate) => HeartRate = heartRate;
    }
}