using AirPlay.Models;
using AirPlay.Models.Audio;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AirPlay;

public interface IAirPlayReceiver
{
    event EventHandler<decimal> OnSetVolumeReceived;
    event EventHandler<H264Data> OnH264DataReceived;
    event EventHandler<PcmData> OnPCMDataReceived;

    event EventHandler<TrackInfoValue> OnTrackInfoValueReceived;

    Task StartListeners(CancellationToken cancellationToken);

    Task StartMdnsAsync();
}
