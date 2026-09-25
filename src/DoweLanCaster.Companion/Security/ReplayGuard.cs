using System.Collections.Concurrent;

namespace DoweLanCaster.Companion.Security;

public sealed class ReplayGuard
{
    private sealed class SessionState
    {
        public readonly object Gate = new();
        public long LastSequence;
        public readonly Dictionary<string, DateTimeOffset> RequestIds = new(StringComparer.Ordinal);
    }

    private readonly ConcurrentDictionary<string, SessionState> _sessions = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    public ReplayGuard(TimeProvider? timeProvider = null) => _timeProvider = timeProvider ?? TimeProvider.System;

    public bool TryAccept(string sessionId, string requestId, long sequence,
        long timestampUnixMilliseconds, TimeSpan allowedClockSkew, out string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(requestId) || sequence <= 0)
        { errorCode = "invalid_request"; return false; }

        var now = _timeProvider.GetUtcNow();
        DateTimeOffset timestamp;
        try { timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampUnixMilliseconds); }
        catch (ArgumentOutOfRangeException) { errorCode = "stale_request"; return false; }
        if ((now - timestamp).Duration() > allowedClockSkew)
        { errorCode = "stale_request"; return false; }

        var state = _sessions.GetOrAdd(sessionId, static _ => new SessionState());
        lock (state.Gate)
        {
            foreach (var expired in state.RequestIds
                         .Where(pair => now - pair.Value > allowedClockSkew + allowedClockSkew)
                         .Select(pair => pair.Key).ToArray())
                state.RequestIds.Remove(expired);
            if (state.RequestIds.ContainsKey(requestId))
            { errorCode = "duplicate_request"; return false; }
            if (sequence <= state.LastSequence)
            { errorCode = "replayed_sequence"; return false; }
            state.RequestIds.Add(requestId, now);
            state.LastSequence = sequence;
            errorCode = null;
            return true;
        }
    }

    public void RemoveSession(string sessionId) => _sessions.TryRemove(sessionId, out _);
}
