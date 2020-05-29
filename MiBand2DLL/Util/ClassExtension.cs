using System;

namespace MiBand2DLL.Util
{
    public static class ClassExtension
    {
        /// <summary>
        /// Creates a subarray of the given array using the provided start index and length of the wanted array.
        /// </summary>
        /// <param name="array">The original array.</param>
        /// <param name="startIndex">Starting index for the subarray.</param>
        /// <param name="subArrayLength">Length of the subarray aka. stopping index.</param>
        /// <returns>Sub array of the given array.</returns>
        public static T[] SubArray<T>(this T[] array, int startIndex, int subArrayLength)
        {
            T[] result = new T[subArrayLength];
            Array.Copy(array, startIndex, result, 0, subArrayLength);
            return result;
        }
    }
}