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
        /// <summary>
        /// Different statuses of the response.
        /// </summary>
        public enum ResponseStatus { Success, Failure }

        /// <summary>
        /// The data of this response.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// The status of this response. See: <see cref="ResponseStatus"/>.
        /// </summary>
        public ResponseStatus Status { get; }

        /// <summary>
        /// The type of the data. Used for deserializing. 
        /// </summary>
        private readonly Type _dataType;

        public ServerResponse(object data, ResponseStatus status = ResponseStatus.Success)
        {
            Data = data;
            Status = status;
            _dataType = Data.GetType();
        }

        public ServerResponse(Exception exception) : this(exception, ResponseStatus.Failure)
        {
        }

        /// <summary>
        /// Creates an a ServerResponse that holds an empty string as data with a successful <see cref="ResponseStatus"/>.
        /// This is considered as an "empty" response, used to simply return a success.
        /// </summary>
        /// <returns>An "empty" successful response.</returns>
        public static ServerResponse EmptySuccess() => new ServerResponse(string.Empty);

        /// <summary>
        /// Converts the ServerResponse to a JSON-String.
        /// </summary>
        /// <returns>The ServerResponse-Object as a JSON-String.</returns>
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

        /// <summary>
        /// Converts a JSON-String (representing a ServerResponse) back to an object.
        /// </summary>
        /// <param name="json">The JSON-String representing the ServerResponse.</param>
        /// <returns>The ServerResponse converted from the JSON-String.</returns>
        /// <exception cref="TypeLoadException">Data has no type. Cannot create ServerStatus with type-less data.</exception>
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