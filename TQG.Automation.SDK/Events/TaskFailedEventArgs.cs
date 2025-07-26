using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Events;

/// <summary>
/// Thông báo sự kiện khi một tác vụ trên thiết bị thất bại.
/// </summary>
/// <param name="deviceId">Thiết bị</param>
/// <param name="taskId">Task đang chạy</param>
/// <param name="errorDetail">Chi tiết lỗi (ErrorCode,ErrorMessage)</param>
public sealed class TaskFailedEventArgs(string deviceId, string taskId, ErrorDetail errorDetail) : EventArgs
{
    public string DeviceId { get; } = deviceId;
    public string TaskId { get; } = taskId;
    public ErrorDetail ErrorDetail { get; } = errorDetail;
}

