namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Cấu hình thiết bị
/// </summary>
public sealed class DeviceProfile
{
    /// <summary>
    /// Định danh duy nhất, không trùng lặp cho thiết bị.
    /// Ví dụ: "SHUTTLE_01"
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Địa chỉ IP của PLC điều khiển thiết bị này.
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Loại CPU của PLC. Mặc định là S7-1200.
    /// </summary>
    public PlcType Cpu { get; init; } = PlcType.S71200;

    /// <summary>
    /// Thông số PLC
    /// </summary>
    public short Rack { get; init; } = 0;

    /// <summary>
    /// Thông số PLC
    /// </summary>
    public short Slot { get; init; } = 1;

    /// <summary>
    ///  Địa chỉ các thanh ghi
    /// </summary>
    public required SignalMap Signals { get; init; }

    /// <summary>
    /// Hỗ trợ nhận lệnh nhập từ hệ thống.
    /// </summary>
    public bool HasSupportInbound { get; init; }

    /// <summary>
    /// Khoảng thời gian polling trạng thái (giây). Mặc định: 1 giây.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 1;

    /// <summary>
    /// Timeout cho các lệnh (phút). Mặc định: 10 phút.
    /// </summary>
    public int TimeoutMinutes { get; init; } = 10;
}
