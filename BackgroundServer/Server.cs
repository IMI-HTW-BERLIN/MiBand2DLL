using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Data;
using Data.ResponseTypes;
using Data.ServerCommunication;
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
        private static ServerWriter _writer;

        /// <summary>
        /// BinaryReader used for reading received commands from the client.
        /// </summary>
        private static ServerReader _reader;

        /// <summary>
        /// Whether we are currently listening for commands. Used as a stopping-token.
        /// </summary>
        private static bool _listenForCommands = true;

        /// <summary>
        /// A list of all connected mi bands.
        /// </summary>
        private static readonly List<MiBand2> MiBands = new List<MiBand2>();

        /// <summary>
        /// Simply starts the server with the execution of the exe.
        /// </summary>
        public static async Task Main(string[] args) => await StartServer();

        /// <summary>
        /// (Re-)Starts the server, waits for a client, initializes the components and waits for input from client.
        /// </summary>
        private static async Task StartServer()
        {
            MiBands.Clear();
            _server?.Stop();
            _listenForCommands = true;
            _server = TcpListener.Create(Consts.ServerData.PORT);
            _server.Start();

            Console.WriteLine("Waiting for client...");
            _client = await _server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            NetworkStream stream = _client.GetStream();
            _writer = new ServerWriter(stream);
            _reader = new ServerReader(stream);
            await ListenForCommands();
        }

        /// <summary>
        /// Waits for commands from client. This blocks the main thread of the server (nothing to do besides this).
        /// </summary>
        private static async Task ListenForCommands()
        {
            try
            {
                while (_listenForCommands)
                {
                    // BinaryReader actually blocks the thread if there is no data in the stream
                    // -> while-loop paused until command received
                    ServerCommand serverCommand = _reader.ReadServerCommand();
                    if (serverCommand == null)
                        continue;
                    Console.WriteLine($"Command: {serverCommand.Command} for device: {serverCommand.DeviceIndex}");
                    await ExecuteCommand(serverCommand);
                }
            }
            catch (IOException exception)
            {
                await ClientConnectionLost(0, exception.Message);
            }
        }

        /// <summary>
        /// Executes the given command. Will send any exception that occures to the client.
        /// </summary>
        /// <param name="serverCommand">The ServerCommand containing the deviceIndex and the command.</param>
        private static async Task ExecuteCommand(ServerCommand serverCommand)
        {
            int deviceIndex = serverCommand.DeviceIndex;
            if (deviceIndex > MiBands.Count - 1)
                MiBands.Add(new MiBand2(MiBands.Count));
            MiBand2 miBand2 = MiBands[deviceIndex];
            try
            {
                switch (serverCommand.Command)
                {
                    case Consts.Command.ConnectBand:
                        await miBand2.ConnectBandAsync();
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.DisconnectBand:
                        miBand2.DisconnectBand();
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.AuthenticateBand:
                        await miBand2.AuthenticateBandAsync();
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.StartMeasurement:
                        await miBand2.StartMeasurementAsync();
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.StopMeasurement:
                        await miBand2.StopMeasurementAsync();
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.SubscribeToHeartRateChange:
                        miBand2.SubscribeToHeartRateChange(OnHeartRateChange);
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.SubscribeToDeviceConnectionStatusChanged:
                        miBand2.DeviceConnectionChanged += OnDeviceConnectionStatusChanged;
                        break;
                    case Consts.Command.AskUserForTouch:
                        await miBand2.AskUserForTouchAsync();
                        SendSuccess(deviceIndex);
                        break;
                    case Consts.Command.StopServer:
                        if (miBand2.Connected)
                            miBand2.DisconnectBand();
                        _listenForCommands = false;
                        _server.Stop();
                        break;
                    default:
                        ArgumentOutOfRangeException exception =
                            new ArgumentOutOfRangeException(nameof(serverCommand.Command), serverCommand.Command,
                                "Could not find command.");
                        SendData(serverCommand.DeviceIndex, new ServerResponse(exception).ToJson());
                        break;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("EXCEPTION OCCURED:");
                Console.WriteLine("Type: {0}\nMessage{1}", exception.GetType(), exception.Message);
                ServerResponse response = new ServerResponse(exception);
                SendData(serverCommand.DeviceIndex, response.ToJson());
            }
        }

        /// <summary>
        /// Sends a <see cref="DeviceConnectionResponse"/> to the client.
        /// </summary>
        /// <param name="isConnected">Whether the device is connected or not.</param>
        private static void OnDeviceConnectionStatusChanged(int deviceIndex, bool isConnected)
        {
            Console.WriteLine("Sending device connection status to client: isConnected = {0}", isConnected);
            ServerResponse response =
                new ServerResponse(new DeviceConnectionResponse(deviceIndex, isConnected));
            SendData(deviceIndex, response.ToJson());
        }

        /// <summary>
        /// Sends a <see cref="HeartRateResponse"/> to the client.
        /// </summary>
        /// <param name="heartRateResponse">The HeartRateResponse holding the measurement data.</param>
        private static void OnHeartRateChange(HeartRateResponse heartRateResponse)
        {
            Console.WriteLine("Sending heart rate to client: {0}", heartRateResponse.HeartRate);
            ServerResponse response = new ServerResponse(heartRateResponse);
            SendData(heartRateResponse.DeviceIndex, response.ToJson());
        }

        /// <summary>
        /// Sends a success response to the client. Used for indicating successful executed commands.
        /// </summary>
        private static void SendSuccess(int deviceIndex)
        {
            Console.WriteLine("Successfully executed command.");
            string json = ServerResponse.EmptySuccess().ToJson();
            SendData(deviceIndex, json);
        }

        /// <summary>
        /// Sends the given data (JSON) to the client. Restarts server if client got lost.
        /// </summary>
        /// <param name="data">The data to be send (as JSON-string)</param>
        /// <param name="deviceIndex">The corresponding MiBandIndex</param>
        private static async void SendData(int deviceIndex, string data)
        {
            Console.WriteLine("Sending: {0}", data);
            try
            {
                _writer.Write(data);
            }
            catch (IOException exception)
            {
                await ClientConnectionLost(deviceIndex, exception.Message);
            }
        }

        /// <summary>
        /// Restarts the server after not being able to reach the client.
        /// </summary>
        /// <param name="exceptionMessage">The exception message that indicates the connection-loss-cause</param>
        /// <param name="deviceIndex">The corresponding MiBandIndex</param>
        /// <returns></returns>
        private static async Task ClientConnectionLost(int deviceIndex, string exceptionMessage)
        {
            Console.WriteLine("Could not reach client.\nException Message: {0}", exceptionMessage);
            Console.WriteLine("Restarting server...");
            MiBands[deviceIndex].DisconnectBand();
            await StartServer();
        }
    }
}