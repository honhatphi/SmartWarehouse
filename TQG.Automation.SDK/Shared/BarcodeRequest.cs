namespace TQG.Automation.SDK.Shared;

public class BarcodeRequest
{
    public required string DeviceId { get; set; }
    public required string TaskId { get; set; }
    public required string Barcode { get; set; }
    public Location? ActualLocation { get; set; }
}
