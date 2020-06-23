using System;
using System.Threading.Tasks;

namespace MiBand2DLL.Util
{
    public static class AsyncExtension
    {
        /// <summary>
        /// Waits until the given predicate is true. Will delay the current Task with the given frequency.
        /// </summary>
        /// <param name="predicate">Condition until which the task will be delayed.</param>
        /// <param name="checkFrequency">Frequency for checking the condition.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> predicate, int checkFrequency = 25)
        {
            while (!predicate.Invoke())
                await Task.Delay(checkFrequency);
        }
    }
}