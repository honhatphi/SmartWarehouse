using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Events;

public class DeviceStatusChangedEventArgs(string deviceId, DeviceStatus newStatus) : EventArgs
{
    public string DeviceId { get; } = deviceId;
    public DeviceStatus NewStatus { get; } = newStatus;
}