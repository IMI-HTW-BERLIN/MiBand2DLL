using System;

namespace MiBand2DLL.lib
{
    public static class ClassExtension
    {
        public static T[] SubArray<T>(this T[] array, int startIndex, int subArrayLength)
        {
            T[] result = new T[subArrayLength];
            Array.Copy(array, startIndex, result, 0, subArrayLength);
            return result;
        }
    }
}