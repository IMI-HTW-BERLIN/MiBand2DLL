using System.Text.RegularExpressions;

namespace Data.ServerCommunication
{
    /// <summary>
    /// A ServerCommand will be send from the client to the server, where the command will be executed.
    /// The ServerCommand includes a devices-index and the command itself.
    /// </summary>
    public class ServerCommand
    {
        /// <summary>
        /// The device index of the device that this command is intended for.
        /// </summary>
        public int DeviceIndex { get; private set; }

        /// <summary>
        /// The command for the device.
        /// </summary>
        public Consts.Command Command { get; private set; }

        private static readonly Regex Regex = new Regex(SEPARATOR);

        private const string SEPARATOR = "-";

        /// <summary>
        /// Creates a ServerCommand object with the given deviceIndex and command.
        /// </summary>
        /// <param name="deviceIndex">The index of the device.</param>
        /// <param name="command">The command for the device.</param>
        public ServerCommand(int deviceIndex, Consts.Command command)
        {
            DeviceIndex = deviceIndex;
            Command = command;
        }

        /// <summary>
        /// Returns a ServerCommand created from the given string.
        /// </summary>
        /// <param name="serverCommandString">The ServerCommand-string including the deviceIndex and the command.</param>
        /// <returns>A ServerCommand created from the given string.</returns>
        public static ServerCommand FromString(string serverCommandString)
        {
            if (string.IsNullOrEmpty(serverCommandString))
                return null;
            string[] data = Regex.Split(serverCommandString);
            return new ServerCommand(int.Parse(data[0]), (Consts.Command) int.Parse(data[1]));
        }

        /// <summary>
        /// Creates a string from the ServerCommand object, using a specific format.
        /// This allows the reverse operation, creating a ServerCommand from this string.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{DeviceIndex}{SEPARATOR}{(int) Command}";
    }
}