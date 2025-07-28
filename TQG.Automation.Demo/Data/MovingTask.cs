using TQG.Automation.SDK.Shared;

namespace TQG.Automation.Demo.Data;

public class MovingTask
{
    public required string TaskId { get; set; }
    public string? Barcode { get; set; } // Inbound
    public CommandType CommandType { get; set; }
    public Location? SourceLocation { get; set; }  // Cho Outbound và Transfer
    public Location? TargetLocation { get; set; }  // Cho Transfer
    public short GateNumber { get; set; }
    public Direction InDirBlock { get; set; }  // Cho Transfer
    public Direction OutDirBlock { get; set; }  // Cho cả Outbound và Transfer
}
