namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Lệnh di chuyển
/// </summary>
public class TransportTask
{

    /// <summary>
    /// Id Lệnh nhập
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// Loại
    /// </summary>
    public CommandType CommandType { get; set; }

    /// <summary>
    /// Vị trí nguồn
    /// </summary>
    public Location? SourceLocation { get; set; }  // Cho Outbound và Transfer

    /// <summary>
    /// Vị trí đích
    /// </summary>
    public Location? TargetLocation { get; set; }  // Cho Transfer

    /// <summary>
    /// Số cửa
    /// </summary>
    public short GateNumber { get; set; }

    /// <summary>
    /// Hướng vào của shuttle
    /// </summary>
    public Direction InDirBlock { get; set; }  // Cho Transfer

    /// <summary>
    /// Hướng ra của shuttle
    /// </summary>
    public Direction OutDirBlock { get; set; }  // Cho cả Outbound và Transfer
}