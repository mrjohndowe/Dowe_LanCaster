namespace DoweLanCaster.Models;

public sealed class RokuMediaPlayerState
{
    public string State { get; init; } = "";
    public double? PositionSeconds { get; init; }
    public double? DurationSeconds { get; init; }

    public bool IsPlaying =>
        State.Equals("play", StringComparison.OrdinalIgnoreCase) ||
        State.Equals("playing", StringComparison.OrdinalIgnoreCase);

    public bool IsStopped =>
        State.Equals("stop", StringComparison.OrdinalIgnoreCase) ||
        State.Equals("stopped", StringComparison.OrdinalIgnoreCase) ||
        State.Equals("close", StringComparison.OrdinalIgnoreCase) ||
        State.Equals("finished", StringComparison.OrdinalIgnoreCase);
}
