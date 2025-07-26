namespace TQG.Automation.SDK.Exceptions;

public sealed class DeviceNotRegisteredException(string deviceId) : Exception($"Device with ID '{deviceId}' not registered.")
{
    public string DeviceId { get; } = deviceId;
}

