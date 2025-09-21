using AirPlay.Models;
using AirPlay.Models.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AirPlay.Services.Implementations;

public partial class SessionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions = [];

    public event EventHandler<Session> OnSessionsAddedOrUpdated;

    public IReadOnlyDictionary<string, Session> Sessions => _sessions;

    public Task<Session> GetSessionAsync(string key)
    {
        _sessions.TryGetValue(key, out Session _session);
        return Task.FromResult(_session ?? new Session(key));
    }

    public Task CreateOrUpdateSessionAsync(string key, Session session)
    {
        _sessions.AddOrUpdate(key, session, (k, old) =>
        {
            session.DacpId ??= old.DacpId;
            session.DacpEndPoint ??= old.DacpEndPoint;
            session.EcdhOurs ??= old.EcdhOurs;
            session.EcdhTheirs ??= old.EcdhTheirs;
            session.EdTheirs ??= old.EdTheirs;
            session.EcdhShared ??= old.EcdhShared;
            session.PairVerified ??= old.PairVerified;
            session.AesKey ??= old.AesKey;
            session.AesIv ??= old.AesIv;
            session.StreamConnectionId ??= old.StreamConnectionId;
            session.KeyMsg ??= old.KeyMsg;
            session.DecryptedAesKey ??= old.DecryptedAesKey;
            session.MirroringListener ??= old.MirroringListener;
            session.AudioControlListener ??= old.AudioControlListener;
            session.SpsPps ??= old.SpsPps;
            session.Pts ??= old.Pts;
            session.WidthSource ??= old.WidthSource;
            session.HeightSource ??= old.HeightSource;
            session.MirroringSession ??= old.MirroringSession;
            session.AudioFormat = session.AudioFormat == AudioFormat.Unknown ? old.AudioFormat : session.AudioFormat;

            return session;
        });

        OnSessionsAddedOrUpdated?.Invoke(this, session);

        return Task.CompletedTask;
    }
}

partial class SessionManager
{
    private static SessionManager _current = null;
    public static SessionManager Current => _current ??= new SessionManager();
}