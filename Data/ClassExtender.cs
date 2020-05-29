using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Data
{
    public static class ClassExtender
    {
        public static string ToJson(this Exception exception)
        {
            Dictionary<string, string> exceptionDictionary = new Dictionary<string, string>
            {
                {"Type", exception.GetType().ToString()},
                {"Message", exception.Message}
            };

            return JsonSerializer.Serialize(exceptionDictionary);
        }
    }
}