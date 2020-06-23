using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Data;
using Data.ResponseTypes;
using MiBand2DLL;

namespace BackgroundServer
{
    internal static class Server
    {
        /// <summary>
        /// The client-reference with which this server is connected.
        /// </summary>
        private static TcpClient _client;

        /// <summary>
        /// The actual server reference.
        /// </summary>
        private static TcpListener _server;

        /// <summary>
        /// BinaryWriter used for writing to the client
        /// </summary>
        private static BinaryWriter _writer;

        /// <summary>
        /// BinaryReader used for reading received commands from the client.
        /// </summary>
        private static BinaryReader _reader;

        /// <summary>
        /// Whether we are currently listening for commands. Used as a stopping-token.
        /// </summary>
        private static bool _listenForCommands = true;

        /// <summary>
        /// Simply starts the server with the execution of the exe.
        /// </summary>
        public static async Task Main(string[] args) => await StartServer();

        /// <summary>
        /// (Re-)Starts the server, waits for a client, initializes the components and waits for input from client.
        /// </summary>
        private static async Task StartServer()
        {
            _server?.Stop();
            _listenForCommands = true;
            _server = TcpListener.Create(Consts.ServerData.PORT);
            _server.Start();

            Console.WriteLine("Waiting for client...");
            _client = await _server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            NetworkStream stream = _client.GetStream();
            _writer = new BinaryWriter(stream, Encoding.UTF8, true);
            _reader = new BinaryReader(stream, Encoding.UTF8, true);
            await ListenForCommands();
        }

        /// <summary>
        /// Waits for commands from client. This blocks the main thread of the server (nothing to do besides this).
        /// </summary>
        private static async Task ListenForCommands()
        {
            using (_reader)
            {
                try
                {
                    while (_listenForCommands)
                    {
                        // BinaryReader actually blocks the thread if there is no data in the stream
                        // -> while-loop paused until command received
                        Consts.Command command = (Consts.Command) _reader.ReadInt32();
                        Console.WriteLine("Command received: {0}.", command);
                        await ExecuteCommand(command);
                    }
                }
                catch (IOException exception)
                {
                    await ClientConnectionLost(exception.Message);
                }
            }
        }

        /// <summary>
        /// Executes the given command. Will send any exception that occures to the client.
        /// </summary>
        /// <param name="command">The command to be executed</param>
        private static async Task ExecuteCommand(Consts.Command command)
        {
            try
            {
                switch (command)
                {
                    case Consts.Command.ConnectBand:
                        await MiBand2.ConnectBandAsync();
                        SendSuccess();
                        break;
                    case Consts.Command.DisconnectBand:
                        MiBand2.DisconnectBand();
                        SendSuccess();
                        break;
                    case Consts.Command.AuthenticateBand:
                        await MiBand2.AuthenticateBandAsync();
                        SendSuccess();
                        break;
                    case Consts.Command.StartMeasurement:
                        await MiBand2.StartMeasurementAsync();
                        SendSuccess();
                        break;
                    case Consts.Command.StopMeasurement:
                        await MiBand2.StopMeasurementAsync();
                        SendSuccess();
                        break;
                    case Consts.Command.SubscribeToHeartRateChange:
                        MiBand2.SubscribeToHeartRateChange(OnHeartRateChange);
                        SendSuccess();
                        break;
                    case Consts.Command.SubscribeToDeviceConnectionStatusChanged:
                        MiBand2.DeviceConnectionChanged += OnDeviceConnectionStatusChanged;
                        break;
                    case Consts.Command.AskUserForTouch:
                        await MiBand2.AskUserForTouchAsync();
                        SendSuccess();
                        break;
                    case Consts.Command.StopServer:
                        if (MiBand2.Connected)
                            MiBand2.DisconnectBand();
                        _listenForCommands = false;
                        _server.Stop();
                        break;
                    default:
                        ArgumentOutOfRangeException exception =
                            new ArgumentOutOfRangeException(nameof(command), command, "Could not find command.");
                        SendData(new ServerResponse(exception).ToJson());
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("EXCEPTION OCCURED:");
                Console.WriteLine("Type: {0}\nMessage{1}", exception.GetType(), exception.Message);
                ServerResponse response = new ServerResponse(exception);
                SendData(response.ToJson());
            }
        }

        /// <summary>
        /// Sends a <see cref="DeviceConnectionResponse"/> to the client.
        /// </summary>
        /// <param name="isConnected">Whether the device is connected or not.</param>
        private static void OnDeviceConnectionStatusChanged(bool isConnected)
        {
            Console.WriteLine("Sending device connection status to client: isConnected = {0}", isConnected);
            ServerResponse response =
                new ServerResponse(new DeviceConnectionResponse(isConnected));
            SendData(response.ToJson());
        }

        /// <summary>
        /// Sends a <see cref="HeartRateResponse"/> to the client.
        /// </summary>
        /// <param name="heartRateResponse">The HeartRateResponse holding the measurement data.</param>
        private static void OnHeartRateChange(HeartRateResponse heartRateResponse)
        {
            Console.WriteLine("Sending heart rate to client: {0}", heartRateResponse.HeartRate);
            ServerResponse response = new ServerResponse(heartRateResponse);
            SendData(response.ToJson());
        }

        /// <summary>
        /// Sends a success response to the client. Used for indicating successful executed commands.
        /// </summary>
        private static void SendSuccess()
        {
            Console.WriteLine("Successfully executed command.");
            string json = ServerResponse.EmptySuccess().ToJson();
            SendData(json);
        }

        /// <summary>
        /// Sends the given data (JSON) to the client. Restarts server if client got lost.
        /// </summary>
        /// <param name="data">The data to be send (as JSON-string)</param>
        private static async void SendData(string data)
        {
            Console.WriteLine("Sending: {0}", data);
            try
            {
                _writer.Write(data);
            }
            catch (IOException exception)
            {
                await ClientConnectionLost(exception.Message);
            }
        }

        /// <summary>
        /// Restarts the server after not being able to reach the client.
        /// </summary>
        /// <param name="exceptionMessage">The exception message that indicates the connection-loss-cause</param>
        /// <returns></returns>
        private static async Task ClientConnectionLost(string exceptionMessage)
        {
            Console.WriteLine("Could not reach client.\nException Message: {0}", exceptionMessage);
            Console.WriteLine("Restarting server...");
            MiBand2.DisconnectBand();
            await StartServer();
        }
    }
}