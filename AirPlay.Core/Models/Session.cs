using AirPlay.Listeners;
using AirPlay.Models.Enums;
using System.Net;

namespace AirPlay.Models;

public class Session(string sessionId)
{
    public string SessionId => sessionId;

    public byte[] EcdhOurs { get; set; } = null;
    public byte[] EcdhTheirs { get; set; } = null;
    public byte[] EdTheirs { get; set; } = null;
    public byte[] EcdhShared { get; set; } = null;

    public bool? PairVerified { get; set; } = null;

    public byte[] KeyMsg { get; set; } = null;
    public byte[] AesKey { get; set; } = null;
    public byte[] AesIv { get; set; } = null;
    public string StreamConnectionId { get; set; } = null;
    public AudioFormat AudioFormat { get; set; } = AudioFormat.Unknown;

    public MirroringListener? MirroringListener = null;
    public StreamingListener? StreamingListener = null;
    public AudioListener? AudioControlListener = null;

    public byte[] DecryptedAesKey { get; set; } = null;
    public byte[] SpsPps { get; set; } = null;
    public long? Pts { get; set; } = null;
    public int? WidthSource { get; set; } = null;
    public int? HeightSource { get; set; } = null;

    public bool? MirroringSession = null;

    public string? DacpId { get; set; } = null;

    public IPEndPoint? DacpEndPoint { get; set; } = null;

    public bool PairCompleted => EcdhShared != null && (PairVerified ?? false);
    public bool FairPlaySetupCompleted => KeyMsg != null && EcdhShared != null && (PairVerified ?? false);
    public bool FairPlayReady => KeyMsg != null && EcdhShared != null && AesKey != null && AesIv != null;
    public bool MirroringSessionReady => StreamConnectionId != null && MirroringSession.HasValue ? MirroringSession.Value : false;
    public bool AudioSessionReady => AudioFormat != AudioFormat.Unknown;
}
