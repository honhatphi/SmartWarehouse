using System;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.Demo;
public sealed class BarcodeExecuteEventArgs(string deviceId, string taskId, string barcode, bool isValid, Location? targetLocation) : EventArgs
{
    public string DeviceId { get; } = deviceId;
    public string TaskId { get; } = taskId;
    public string Barcode { get; } = barcode;
    public bool IsValid { get; } = isValid;
    public Location? TargetLocation { get; } = targetLocation;
}