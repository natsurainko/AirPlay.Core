namespace AirPlay.Models.Audio;

public enum TrackInfoType
{
    Name,
    Artist,
    Album,
    Cover,
    ProgressDuration,
    ProgressPosition,
}

public record struct TrackInfoValue(TrackInfoType Type, object Value);