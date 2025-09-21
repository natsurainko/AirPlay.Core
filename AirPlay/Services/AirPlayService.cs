using AirPlay.Models.Configs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AirPlay.Services;

public class AirPlayService(IAirPlayReceiver airPlayReceiver, IOptions<DumpConfig> dConfig) : IHostedService 
{
    private readonly IAirPlayReceiver _airPlayReceiver = airPlayReceiver ?? throw new ArgumentNullException(nameof(airPlayReceiver));
    private readonly DumpConfig _dConfig = dConfig?.Value ?? throw new ArgumentNullException(nameof(dConfig));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _airPlayReceiver.StartListeners(cancellationToken);
        await _airPlayReceiver.StartMdnsAsync().ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
