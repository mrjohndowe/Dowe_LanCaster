using System.Text.Json;

namespace DoweLanCaster.Companion.Contracts;

public sealed record SignedCommandRequest(
    int ProtocolVersion, string ServerId, string SessionId, string DeviceId,
    string RequestId, long Sequence, long TimestampUnixMilliseconds,
    CompanionCommandType Command, JsonElement Arguments, string SignatureBase64);

public sealed record CompanionCommandResult(
    bool Success, string? ErrorCode, string Message, long StateVersion,
    string? OperationId = null);

public sealed record CompanionStateSnapshot(
    int ProtocolVersion, string ServerId, long StateVersion, CompanionTab ActiveTab,
    string? SelectedRokuId, IReadOnlyList<string> Capabilities,
    IReadOnlyList<CompanionCommandType> AllowedCommands);
