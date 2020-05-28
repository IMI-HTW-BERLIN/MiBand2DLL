using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundServer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TcpListener _server;
        private TcpClient _client;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _server = TcpListener.Create(4000);
            _server.Start();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogCritical("Waiting for client...");
            _client = await _server.AcceptTcpClientAsync();
            _logger.LogCritical("Client connected!");
            NetworkStream stream = _client.GetStream();
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                while (true)
                {
                    _logger.LogCritical(reader.ReadString());
                    if (reader.ReadString() == "Stop this madness!")
                        break;
                }
            }
            _server.Stop();
        }
    }
}