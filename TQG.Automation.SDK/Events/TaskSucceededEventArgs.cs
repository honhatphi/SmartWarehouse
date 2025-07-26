namespace TQG.Automation.SDK.Events;

/// <summary>
/// Sự kiện khi một tác vụ trên thiết bị thành công.
/// </summary>
/// <param name="deviceId">Thiết bị</param>
/// <param name="taskId">Task đang chạy</param>
public class TaskSucceededEventArgs(string deviceId, string taskId) : EventArgs
{
    public string DeviceId { get; } = deviceId;
    public string TaskId { get; } = taskId;
}