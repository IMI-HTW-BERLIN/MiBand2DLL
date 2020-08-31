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

        private Task<string> _readingTask;

        /// <summary>
        /// Creates a new ServerReader with the given stream and allows to read from it.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public ServerReader(Stream stream) => _reader = new BinaryReader(stream, Encoding.UTF8, true);

        /// <summary>
        /// Reads the next ServerCommand from the stream. Can return null.
        /// <para>
        /// CAUTION: This will block the current thread. Use <see cref="StartReadTaskAsync"/> to not block it.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public ServerCommand ReadServerCommand()
        {
            using (_reader) return ServerCommand.FromString(_reader.ReadString());
        }

        /// <summary>
        /// Starts the task of reading async from the stream.
        /// </summary>
        public void StartReadTaskAsync()
        {
            using (_reader) _readingTask = _reader.ReadStringAsync();
        }

        /// <summary>
        /// Returns the current completion state of the reading task.
        /// </summary>
        /// <returns></returns>
        public bool IsReadTaskCompleted() => _readingTask.IsCompleted;

        /// <summary>
        /// Returns the ServerCommand read from the stream.
        /// Should only be called after <see cref="IsReadTaskCompleted"/> returns true.
        /// </summary>
        /// <returns>The ServerCommand object received from the stream.</returns>
        public ServerCommand FinishReadTaskAsync() =>
            _readingTask.IsCompleted ? ServerCommand.FromString(_readingTask.Result) : null;
    }
}