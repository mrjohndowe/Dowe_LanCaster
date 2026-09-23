namespace DoweLanCaster.Models;

public sealed record RokuAudioDeviceState(
    int? Volume,
    bool? IsMuted,
    IReadOnlyList<string> Destinations);
