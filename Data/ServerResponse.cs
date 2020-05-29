using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Data
{
    public class ServerResponse
    {
        public Type Type { get; }
        public string Data { get; }

        public bool IsException => Type.IsSubclassOf(typeof(Exception));
        public bool IsInteger => Type == typeof(int);

        public ServerResponse(Type type, string data)
        {
            Type = type;
            Data = data;
        }

        public ServerResponse(string data) => Data = data;

        public string ToJson()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>
            {
                {"Type", Type.ToString()},
                {"Data", Data}
            };
            return JsonSerializer.Serialize(dictionary);
        }

        public static ServerResponse FromJson(string json)
        {
            Dictionary<string, string> dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            Type type = Type.GetType(dictionary["Type"]);
            return new ServerResponse(type, dictionary["Data"] );
        }
    }
}