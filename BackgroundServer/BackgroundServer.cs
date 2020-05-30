using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data;
using Data.CustomExceptions;
using Data.CustomExceptions.HardwareRelatedExceptions;
using MiBand2DLL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundServer
{
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
            _client = await _server.AcceptTcpClientAsync();
            NetworkStream stream = _client.GetStream();
            _writer = new BinaryWriter(stream, Encoding.UTF8, true);
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
            
            while (_receiveCommands)
            {
                Consts.Command command = (Consts.Command) reader.ReadInt32();
                _logger.LogCritical(command.ToString());
                await ExecuteCommand(command);
            }
        }

        private async Task ExecuteCommand(Consts.Command command)
        {
            try
            {
                switch (command)
                {
                    case Consts.Command.ConnectBand:
                        await MiBand2.ConnectBandAsync();
                        break;
                    case Consts.Command.DisconnectBand:
                        MiBand2.DisconnectBand();
                        break;
                    case Consts.Command.AuthenticateBand:
                        await MiBand2.AuthenticateBandAsync();
                        break;
                    case Consts.Command.StartMeasurement:
                        await MiBand2.StartMeasurementAsync();
                        break;
                    case Consts.Command.StopMeasurement:
                        await MiBand2.StopMeasurementAsync();
                        break;
                    case Consts.Command.SubscribeToHeartRateChange:
                        MiBand2.SubscribeToHeartRateChange(OnHeartRateChange);
                        break;
                    case Consts.Command.AskUserForTouch:
                        await MiBand2.AskUserForTouchAsync();
                        break;
                    case Consts.Command.StopServer:
                        _receiveCommands = false;
                        _server.Stop();
                        Dispose();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), command, null);
                }
            }
            catch (Exception e) when(e is ICustomException)
            {
                ServerResponse response = new ServerResponse(e.GetType(), e.Message);
                SendData(response.ToJson());
            }
        }

        private void OnHeartRateChange(int newHeartRate)
        {
            ServerResponse response = new ServerResponse(typeof(int), newHeartRate.ToString());
            SendData(response.ToJson());
        }

        private void SendData(string data) => _writer.Write(data);
    }
}