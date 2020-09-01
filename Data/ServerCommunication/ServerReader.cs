using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Data.ServerCommunication
{
    /// <summary>
    /// A small class that allows to read from the server.
    /// Used as an interface to communicate correctly with the Server.
    /// </summary>
    public class ServerReader
    {
        private readonly BinaryReader _reader;

        /// <summary>
        /// Creates a new ServerReader with the given stream and allows to read from it.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public ServerReader(Stream stream) => _reader = new BinaryReader(stream, Encoding.UTF8, true);

        /// <summary>
        /// Reads the next ServerCommand from the stream. Can return null.
        /// <para>
        /// CAUTION: This will block the current thread.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public ServerCommand ReadServerCommand() => ServerCommand.FromString(_reader.ReadString());

        /// <summary>
        /// Reads from the stream using a task.
        /// </summary>
        public async Task<string> ReadStringAsync() => await _reader.ReadStringAsync();
    }
}