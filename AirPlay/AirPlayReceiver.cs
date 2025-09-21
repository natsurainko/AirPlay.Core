﻿using AirPlay.Listeners;
using AirPlay.Models;
using AirPlay.Models.Audio;
using AirPlay.Models.Configs;
using Makaretu.Dns;
using Microsoft.Extensions.Options;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AirPlay;

public partial class AirPlayReceiver : IRtspReceiver, IAirPlayReceiver
{
    public event EventHandler<decimal> OnSetVolumeReceived;
    public event EventHandler<H264Data> OnH264DataReceived;
    public event EventHandler<PcmData> OnPCMDataReceived;
    public event EventHandler<TrackInfoValue> OnTrackInfoValueReceived;

    public const string AirPlayType = "_airplay._tcp";
    public const string AirTunesType = "_raop._tcp";

    private readonly MulticastService _mdns;
    private readonly AirTunesListener _airTunesListener = null;
    private readonly string _instance;
    private readonly ushort _airTunesPort;
    private readonly ushort _airPlayPort;
    private readonly string _deviceId;

    public AirPlayReceiver(MulticastService mdns, IOptions<AirPlayReceiverConfig> aprConfig, IOptions<CodecLibrariesConfig> codecConfig, IOptions<DumpConfig> dumpConfig)
    {
        _airTunesPort = aprConfig?.Value?.AirTunesPort ?? 5000;
        _airPlayPort = aprConfig?.Value?.AirPlayPort ?? 7000;
        _deviceId = aprConfig?.Value?.DeviceMacAddress ?? "11:22:33:44:55:66";
        _instance = aprConfig?.Value?.Instance ?? throw new ArgumentNullException("apr.instance");
        _mdns = mdns ?? throw new ArgumentNullException(nameof(mdns));

        var clConfig = codecConfig?.Value ?? throw new ArgumentNullException(nameof(codecConfig));
        var dConfig = dumpConfig?.Value ?? throw new ArgumentNullException(nameof(dumpConfig));

        _airTunesListener = new AirTunesListener(this, _airTunesPort, _airPlayPort, clConfig, dConfig);
    }

    public async Task StartListeners(CancellationToken cancellationToken)
    {
        await _airTunesListener.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StartMdnsAsync()
    {
        if (string.IsNullOrWhiteSpace(_deviceId))
        {
            throw new ArgumentNullException(_deviceId);
        }

        var rDeviceId = MacRegex();
        var mDeviceId = rDeviceId.Match(_deviceId);
        if (!mDeviceId.Success)
        {
            throw new ArgumentException("Device id must be a mac address", _deviceId);
        }

        var deviceIdInstance = string.Join(string.Empty, mDeviceId.Groups[2].Captures) + mDeviceId.Groups[3].Value;
        var sd = new ServiceDiscovery(_mdns);

        foreach (var ip in MulticastService.GetIPAddresses())
        {
            Console.WriteLine($"IP address {ip}");
        }

        _mdns.NetworkInterfaceDiscovered += (s, e) =>
        {
            foreach (var nic in e.NetworkInterfaces)
            {
                Console.WriteLine($"NIC '{nic.Name}'");
            }
        };

        // Internally 'ServiceProfile' create the SRV record
        var airTunes = new ServiceProfile($"{deviceIdInstance}@{_instance}", AirTunesType, _airTunesPort);
        airTunes.AddProperty("ch", "2");
        airTunes.AddProperty("cn", "1,2"); // 0=pcm, 1=alac, 2=aac, 3=aac-eld (not supported here)
        airTunes.AddProperty("et", "0,3,5"); // 0=none, 1=rsa (airport express), 3=fairplay, 4=MFiSAP, 5=fairplay SAPv2.5
        airTunes.AddProperty("md", "0,1,2"); // 0=text, 1=artwork, 2=progress
        airTunes.AddProperty("sr", "44100"); // sample rate
        airTunes.AddProperty("ss", "16"); // bitdepth
        airTunes.AddProperty("da", "true"); // unk
        airTunes.AddProperty("sv", "false"); // unk
        airTunes.AddProperty("ft", "0x5A7FDE40,0x1C"); // originally "0x5A7FFFF7,0x1E" https://openairplay.github.io/airplay-spec/features.html
        airTunes.AddProperty("am", "AppleTV5,3");
        airTunes.AddProperty("pk", "29fbb183a58b466e05b9ab667b3c429d18a6b785637333d3f0f3a34baa89f45e");
        airTunes.AddProperty("sf", "0x4");
        airTunes.AddProperty("tp", "UDP");
        airTunes.AddProperty("vn", "65537");
        airTunes.AddProperty("vs", "220.68");
        airTunes.AddProperty("vv", "2");

        /*
         * ch	2	audio channels: stereo
         * cn	0,1,2,3	audio codecs
         * et	0,3,5	supported encryption types
         * md	0,1,2	supported metadata types
         * pw	false	does the speaker require a password?
         * sr	44100	audio sample rate: 44100 Hz
         * ss	16	audio sample size: 16-bit
         */

        // Internally 'ServiceProfile' create the SRV record
        var airPlay = new ServiceProfile(_instance, AirPlayType, _airPlayPort);
        airPlay.AddProperty("deviceid", _deviceId);
        airPlay.AddProperty("features", "0x5A7FDE40,0x1C"); // originally "0x5A7FFFF7,0x1E" https://openairplay.github.io/airplay-spec/features.html
        airPlay.AddProperty("flags", "0x4");
        airPlay.AddProperty("model", "AppleTV5,3");
        airPlay.AddProperty("pk", "29fbb183a58b466e05b9ab667b3c429d18a6b785637333d3f0f3a34baa89f45e");
        airPlay.AddProperty("pi", "aa072a95-0318-4ec3-b042-4992495877d3");
        airPlay.AddProperty("srcvers", "220.68");
        airPlay.AddProperty("vv", "2");

        sd.Advertise(airTunes);
        sd.Advertise(airPlay);

        _mdns.Start();

        return Task.CompletedTask;
    }

    public void OnSetVolume(decimal volume)
    {
        OnSetVolumeReceived?.Invoke(this, volume);
    }

    public void OnData(H264Data data)
    {
        OnH264DataReceived?.Invoke(this, data);
    }

    public void OnPCMData(PcmData data)
    {
        OnPCMDataReceived?.Invoke(this, data);
    }

    public void OnTrackInfoValue(TrackInfoValue infoValue)
    {
        OnTrackInfoValueReceived?.Invoke(this, infoValue);
    }

    [GeneratedRegex("^(([0-9a-fA-F][0-9a-fA-F]):){5}([0-9a-fA-F][0-9a-fA-F])$")]
    private static partial Regex MacRegex();
}