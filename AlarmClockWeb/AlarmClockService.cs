using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClockWeb
{
    public class AlarmClockService : IHostedService
    {
        private readonly CancellationToken _cancellationToken;

        public AlarmClockService(IHostApplicationLifetime applicationLifetime)
        {
            _cancellationToken = applicationLifetime.ApplicationStopping;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Monitor());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Monitor()
        {
            // while (!_cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(100);
                //Console.WriteLine("Monitor ---- ");
            }
        }
    }
}
