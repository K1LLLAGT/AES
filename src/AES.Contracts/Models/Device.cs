namespace AES.Contracts.Models;

public record Device(
    string Serial,
    string? Model,
    string? AndroidVersion,
    string? BuildFingerprint,
    DeviceState State,
    bool IsRooted,
    bool HasFridaServer
);

public enum DeviceState { Offline, Unauthorized, Online, Fastboot }