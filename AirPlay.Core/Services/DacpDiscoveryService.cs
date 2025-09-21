using AirPlay.Models;
using AirPlay.Services.Implementations;
using Makaretu.Dns;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AirPlay.Services;

public class DacpDiscoveryService(MulticastService mdns) : IHostedService
{
    private readonly HttpClient _httpClient = new();
    private readonly MulticastService _mdns = mdns ?? throw new ArgumentNullException(nameof(mdns));
    private ServiceDiscovery? _serviceDiscovery;

    private readonly ConcurrentDictionary<string, (DomainName, IPEndPoint)> _dacpServices = [];

    public event EventHandler<IPEndPoint>? OnDacpServiceShutdown;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceDiscovery = new ServiceDiscovery(_mdns);
        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;

        SessionManager.Current.OnSessionsAddedOrUpdated += OnSessionsAddedOrUpdated;
        return Task.CompletedTask;
    }

    private void OnSessionsAddedOrUpdated(object? sender, Session e)
    {
        if (e.DacpId == null) return;

        if (_dacpServices.TryGetValue(e.DacpId, out var dacpService))
            e.DacpEndPoint = dacpService.Item2;
    }

    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        foreach (var item in e.Message.Answers)
            Debug.WriteLine(item);

        if (e.Message.Answers.Any(a => a is SRVRecord && a.CanonicalName.StartsWith($"iTunes_Ctrl_", StringComparison.OrdinalIgnoreCase)))
        {
            SRVRecord sRVRecord = e.Message.Answers.OfType<SRVRecord>()
                .First(a => a is not null);

            AddressRecord? addressRecord = e.Message.AdditionalRecords.OfType<AddressRecord>()
                .Concat(e.Message.Answers.OfType<AddressRecord>())
                .FirstOrDefault(a => a.Name == sRVRecord.Target && a.Type == DnsType.A);

            if (addressRecord == null) return;

            string dacpId = sRVRecord.Name.Labels[0].Replace("iTunes_Ctrl_", string.Empty);
            IPEndPoint iPEndPoint = new(addressRecord.Address, sRVRecord.Port);

            _dacpServices.AddOrUpdate(dacpId, (e.ServiceInstanceName, iPEndPoint), 
                (key, oldValue) => (e.ServiceInstanceName, iPEndPoint));
        }
    }

    private void OnServiceInstanceShutdown(object? sender, ServiceInstanceShutdownEventArgs e)
    {
        var dacpId = _dacpServices.FirstOrDefault(kv => kv.Value.Item1 == e.ServiceInstanceName);

        if (_dacpServices.TryRemove(dacpId.Key, out var kvp))
        {
            foreach (var session in SessionManager.Current.Sessions.Values.Where(s => s.DacpId == dacpId.Key))
                session.DacpEndPoint = null;

            OnDacpServiceShutdown?.Invoke(this, kvp.Item2);
        }
    }

    public async Task SendCommandAsync(Session session, string command)
    {
        if (session.DacpEndPoint == null)
            throw new InvalidOperationException("DACP service not found.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{session.DacpEndPoint.Address}:{session.DacpEndPoint.Port}/ctrl-int/1/{command}");
        request.Headers.Add("Active-Remote", session.SessionId);

        using var _ = await _httpClient.SendAsync(request);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _serviceDiscovery?.Dispose();
        _mdns.Stop();
        _mdns.Dispose();

        return Task.CompletedTask;
    }
}
