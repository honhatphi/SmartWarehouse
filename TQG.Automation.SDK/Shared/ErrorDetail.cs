namespace TQG.Automation.SDK.Shared;

public record ErrorDetail(int ErrorCode, string ErrorMessage)
{
    public static ErrorDetail NotFoundTask(string deviceId, string taskId) =>
        new(1001, $"No pending response found for task {taskId} on device {deviceId}.");

    public static ErrorDetail MismatchedDevice(string taskId, string expectedDeviceId, string providedDeviceId) =>
        new(1002, $"Mismatched device ID for task {taskId}. Expected: {expectedDeviceId}, Provided: {providedDeviceId}.");

    public static ErrorDetail DeviceNotRegistered(string deviceId) =>
        new(1003, $"Device {deviceId} is not registered in the system.");

    public static ErrorDetail PollingException(string pollType, string deviceId, string taskId, Exception exception)
    => new(1004, $"Polling {pollType} exception for task {taskId} on device {deviceId}: {exception.Message}.");

    public static ErrorDetail TransportTaskAssignmentFailure(string deviceId, string taskId, Exception exception)
        => new(1005, $"Transport task assignment failed for task {taskId} on device {deviceId}: {exception.Message}.");

    public static ErrorDetail CommandReject(short errorCode) =>
        new(errorCode, $"Command rejected with error code {errorCode}.");

    public static ErrorDetail RunningFailure(string taskId, string deviceId, short errorCode)
        => new(errorCode, $"Task {taskId} on device {deviceId} running failed with error code {errorCode}.");


}