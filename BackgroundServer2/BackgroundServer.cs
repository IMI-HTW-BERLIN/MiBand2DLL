using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data;
using Data.ResponseTypes;
using MiBand2DLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundServer
{
    /// <summary>
    /// Will execute commands that are send as int values (corresponding with <see cref="Data.Consts.Command"/>).
    /// Returns a JSON containing a <see cref="ServerResponse"/> with data.
    /// </summary>
    public class BackgroundServer : BackgroundService
    {
        private readonly ILogger<BackgroundServer> _logger;
        private readonly TcpListener _server;
        private TcpClient _client = new TcpClient();
        private BinaryWriter _writer;

        private bool _receiveCommands = true;

        public BackgroundServer(ILogger<BackgroundServer> logger)
        {
            _logger = logger;
            _server = TcpListener.Create(Consts.ServerData.PORT);
            _server.Start();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await MiBand2.ConnectBandAsync();
            await MiBand2.AuthenticateBandAsync();

            
        }

        private async Task ExecuteCommand(Consts.Command command)
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
                        _receiveCommands = false;
                        _server.Stop();
                        Dispose();
                        break;
                    default:
                        ArgumentOutOfRangeException exception =
                            new ArgumentOutOfRangeException(nameof(command), command, null);
                        SendData(new ServerResponse(exception).ToJson());
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical("Exception occured of type:" + e.GetType());
                ServerResponse response = new ServerResponse(e);
                SendData(response.ToJson());
                throw;
            }
        }

        private void OnDeviceConnectionStatusChanged(bool isConnected)
        {
            ServerResponse response =
                new ServerResponse(new DeviceConnectionResponse(isConnected));
            SendData(response.ToJson());
        }

        private void OnHeartRateChange(int newHeartRate)
        {
            ServerResponse response = new ServerResponse(new HeartRateResponse(newHeartRate));
            SendData(response.ToJson());
        }

        private void SendData(string data) => _writer.Write(data);

        private void SendSuccess()
        {
            string json = new ServerResponse(string.Empty).ToJson();
            _logger.LogCritical(json);
            _writer.Write(json);
        }
    }
}