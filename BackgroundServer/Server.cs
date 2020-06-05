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
        private static TcpClient _client;
        private static TcpListener _server;

        private static BinaryWriter _writer;
        private static BinaryReader _reader;

        private static bool _listenForCommands = true;


        public static async Task Main(string[] args)
        {
            await StartServer();
            await ListenForCommands();
        }

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
        }

        private static async Task ListenForCommands()
        {
            using (_reader)
            {
                try
                {
                    while (_listenForCommands)
                    {
                        Consts.Command command = (Consts.Command) _reader.ReadInt32();
                        Console.WriteLine("Command received: {0}.", command);
                        await ExecuteCommand(command);
                    }
                }
                catch (ObjectDisposedException exception)
                {
                    await ClientConnectionLost(exception.Message);
                }
            }
        }

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
                // Needed?
                throw;
            }
        }

        private static void OnDeviceConnectionStatusChanged(bool isConnected)
        {
            Console.WriteLine("Sending device connection status to client: isConnected = {0}", isConnected);
            ServerResponse response =
                new ServerResponse(new DeviceConnectionResponse(isConnected));
            SendData(response.ToJson());
        }

        private static void OnHeartRateChange(int newHeartRate)
        {
            Console.WriteLine("Sending heart rate to client: {0}", newHeartRate);
            ServerResponse response = new ServerResponse(new HeartRateResponse(newHeartRate));
            SendData(response.ToJson());
        }

        private static void SendSuccess()
        {
            Console.WriteLine("Successfully executed command.");
            string json = new ServerResponse(string.Empty).ToJson();
            SendData(json);
        }

        private static async void SendData(string data)
        {
            Console.WriteLine("Sending: {0}", data);
            try
            {
                _writer.Write(data);
            }
            catch (ObjectDisposedException exception)
            {
                ClientConnectionLost(exception.Message).Wait();
            }
        }

        private static async Task ClientConnectionLost(string exceptionMessage)
        {
            Console.WriteLine("Could not reach client.\nException Message: {0}", exceptionMessage);
            Console.WriteLine("Restarting server...");
            await StartServer();
        }
    }
}