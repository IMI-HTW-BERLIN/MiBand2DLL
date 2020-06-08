using System;
using System.Collections.Generic;
using Data.CustomExceptions;
using Data.ResponseTypes;
using Newtonsoft.Json;

namespace Data
{
    /// <summary>
    /// Used for sending responses from the server to the client. Includes following responses:
    /// <para><see cref="ICustomException"/>: If an exception occured. Will include the exception message as data.</para>
    /// <para><see cref="DeviceConnectionResponse"/>: If subscribed to event and whenever device connection status
    /// changes. Will include whether the device is now disconnected or connected.</para>
    /// <para><see cref="HeartRateResponse"/> If subscribed to event and whenever a new heart rate is measured.</para>
    /// </summary>
    public class ServerResponse
    {
        public enum ResponseStatus { Success, Failure }

        public object Data { get; }
        public ResponseStatus Status { get; }

        private Type _dataType;

        public ServerResponse(object data, ResponseStatus status = ResponseStatus.Success)
        {
            Data = data;
            Status = status;
            _dataType = Data.GetType();
        }

        public ServerResponse(Exception exception) : this(exception, ResponseStatus.Failure)
        {
        }

        public string ToJson()
        {
            Dictionary<string, string> jsonDictionary = new Dictionary<string, string>()
            {
                {"DataType", _dataType.ToString()},
                {"Data2", JsonConvert.SerializeObject(Data)},
                {"ResponseStatus", Status.ToString()}
            };
            return JsonConvert.SerializeObject(jsonDictionary);
        }

        public static ServerResponse FromJson(string json)
        {
            Dictionary<string, string> jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            Type dataType = Type.GetType(jsonDictionary["DataType"]);
            if (dataType == null)
                throw new TypeLoadException("Could not find type while deserializing JSON.");
            object data = JsonConvert.DeserializeObject(jsonDictionary["Data2"], dataType);
            return new ServerResponse(data,
                (ResponseStatus) Enum.Parse(typeof(ResponseStatus), jsonDictionary["ResponseStatus"]));
        }
    }
}