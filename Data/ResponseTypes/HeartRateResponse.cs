using System;

namespace Data.ResponseTypes
{
    /// <summary>
    /// A small struct used for holding data related to the measurement of a heart rate.
    /// </summary>
    [Serializable]
    public struct HeartRateResponse
    {
        /// <summary>
        /// The heart rate of this response.
        /// </summary>
        public int HeartRate { get; private set; }

        /// <summary>
        /// Whether this measurement is a repetition of the last one.
        /// (Band does this by default if no new heart rate is measured)
        /// </summary>
        public bool IsRepeating { get; private set; }

        /// <summary>
        /// The time the measurement took.
        /// </summary>
        public long MeasureTime { get; private set; }

        public HeartRateResponse(int heartRate, bool isRepeating, long measureTime)
        {
            HeartRate = heartRate;
            IsRepeating = isRepeating;
            MeasureTime = measureTime;
        }
    }
}