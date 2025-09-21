using AirPlay.Models;
using AirPlay.Models.Audio;

namespace AirPlay;

public interface IRtspReceiver
{
    void OnSetVolume(decimal volume);
    void OnData(H264Data data);
    void OnPCMData(PcmData data);
    void OnTrackInfoValue(TrackInfoValue infoValue);
}
