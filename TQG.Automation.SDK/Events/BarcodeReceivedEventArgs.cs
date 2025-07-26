namespace TQG.Automation.SDK.Events;

/// <summary>
/// Sự kiện khi có một mã vạch được nhận từ thiết bị PLC.
/// </summary>
/// <param name="deviceId">Id thiết bị PLC</param>
/// <param name="taskId">Task đang xử lý</param>
/// <param name="barcode">Mã vạch (10 ký tự vd: 0000001234)</param>
public sealed class BarcodeReceivedEventArgs(string deviceId, string taskId, string barcode) : EventArgs
{
    public string DeviceId { get; } = deviceId;
    public string TaskId { get; } = taskId;
    public string Barcode { get; } = barcode;
}
