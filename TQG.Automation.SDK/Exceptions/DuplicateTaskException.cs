namespace TQG.Automation.SDK.Exceptions;

public sealed class DuplicateTaskException(string taskId) : Exception($"Task {taskId} already exists in queue.")
{
    public string TaskId { get; } = taskId;
}
