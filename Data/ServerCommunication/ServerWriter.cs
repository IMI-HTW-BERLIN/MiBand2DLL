using System.IO;
using System.Text;

namespace Data.ServerCommunication
{
    /// <summary>
    /// A small class that allows to write to the server.
    /// Used as an interface to communicate correctly with the Server.
    /// </summary>
    public class ServerWriter
    {
        private readonly BinaryWriter _writer;

        /// <summary>
        /// Creates a new ServerWriter with the given stream and allows to write with it.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public ServerWriter(Stream stream) => _writer = new BinaryWriter(stream, Encoding.UTF8, true);

        /// <summary>
        /// Writes the given string to the stream.
        /// </summary>
        /// <param name="data">The string data to be written to the stream.</param>
        public void Write(string data)
        {
            using (_writer) _writer.Write(data);
        }

        /// <summary>
        /// Writes the given ServerCommand to the stream.
        /// </summary>
        /// <param name="serverCommand">The ServerCommand to be written to the stream.</param>
        public void WriteServerCommand(ServerCommand serverCommand)
        {
            using (_writer) _writer.Write(serverCommand.ToString());
        }
    }
}