namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Địa chỉ các thanh ghi
/// </summary>
public sealed class SignalMap
{
    #region --- Lệnh ---

    /// <summary>
    /// Lệnh nhập
    /// Biến: Req_ImportPallet | Kiểu: Bool
    /// </summary>
    public required string InboundCommand { get; init; }

    /// <summary>
    /// Lệnh xuất
    /// Biến: Req_ExportPallet | Kiểu: Bool
    /// </summary>
    public required string OutboundCommand { get; init; }

    /// <summary>
    /// Lệnh chuyển
    /// Biến: Req_TransferPallet | Kiểu: Bool
    /// </summary>
    public required string TransferCommand { get; init; }

    /// <summary>
    /// Lệnh bắt đầu quá trình
    /// Biến: Req_StartProcess | Kiểu: Bool
    /// </summary>
    public required string StartProcessCommand { get; init; }

    #endregion

    #region --- Trạng thái ---

    /// <summary>
    /// Lệnh đã được xác nhận
    /// Biến: Status_ShuttleBusy | Kiểu: Bool
    /// </summary>
    public required string CommandAcknowledged { get; init; }

    /// <summary>
    /// Lệnh đã bị từ chối
    /// Biến: Status_InvalidPosition | Kiểu: Bool
    /// </summary>
    public required string CommandRejected { get; init; }

    /// <summary>
    /// Hoàn thành lệnh nhập
    /// Biến: Done_ImportProcess | Kiểu: Bool
    /// </summary>
    public required string InboundComplete { get; init; }

    /// <summary>
    /// Hoàn thành lệnh xuất
    /// Biến: Done_ExportProcess | Kiểu: Bool
    /// </summary>
    public required string OutboundComplete { get; init; }

    /// <summary>
    /// Hoàn thành lệnh chuyển
    /// Biến: Done_TransferProcess | Kiểu: Bool
    /// </summary>
    public required string TransferComplete { get; init; }

    /// <summary>
    /// Lỗi trong quá trình thực hiện lệnh
    /// Biến: Error_Running | Kiểu: Bool
    /// </summary>
    public required string Alarm { get; init; }

    #endregion

    #region --- Cửa xuất/nhập & Điều hướng vào Tầng 3

    /// <summary>
    /// Hướng xuất ra 
    /// True: Ra phía trên (Direction = Top), False: Ra phía dưới (Direction = Bottom)
    /// Biến: Dir_Src_Block3 | Kiểu: Bool
    /// </summary>
    public required string OutDirBlock { get; init; }

    /// <summary>
    /// Hướng nhập vào
    /// True: Ra phía trên (Direction = Top), False: Ra phía dưới (Direction = Bottom)
    /// Biến: Dir_Taget_Block3 | Kiểu: Bool
    /// </summary>
    public required string InDirBlock { get; init; }

    /// <summary>
    /// Cửa xuất/nhập
    /// Biến: Port_IO_Number | Kiểu: Int
    /// </summary>
    public required string GateNumber { get; init; }

    #endregion

    #region --- Dữ liệu vị trí Nguồn ---

    /// <summary>
    /// Tầng nguồn
    /// Biến: Source_Floor | Kiểu: Int
    /// </summary>
    public required string SourceFloor { get; init; }

    /// <summary>
    /// Dãy nguồn
    /// Biến: Source_Rail | Kiểu: Int
    /// </summary>
    public required string SourceRail { get; init; }

    /// <summary>
    /// Kệ nguồn
    /// Biến: Source_Block | kiểu: Int
    /// </summary>
    public required string SourceBlock { get; init; }

    #endregion

    #region --- Dữ liệu vị trí Đích ---

    /// <summary>
    /// Tầng đích
    /// Biến: Target_Floor | Kiểu: Int
    /// </summary>
    public required string TargetFloor { get; init; }

    /// <summary>
    /// Dãy đích
    /// Biến: Target_Rail | Kiểu: Int
    /// </summary>
    public required string TargetRail { get; init; }

    /// <summary>
    /// Kệ đích
    /// Biến: Target_Block | Kiểu : Int
    /// </summary>
    public required string TargetBlock { get; init; }

    #endregion

    #region --- Phản hồi ---

    /// <summary>
    /// Barcode hợp lệ
    /// Biến: Barcode_Valid | Kiểu: Bool
    /// </summary>
    public required string BarcodeValid { get; init; }

    /// <summary>
    /// Barcode không hợp lệ
    /// Biến: Barcode_Invalid | Kiểu: Bool
    /// </summary>
    public required string BarcodeInvalid { get; init; }

    /// <summary>
    /// Tầng thực tế
    /// Biến: Cur_Shuttle_Floor | Kiểu: Int
    /// </summary>
    public required string ActualFloor { get; init; }

    /// <summary>
    /// Dãy thực tế
    /// Biến: Cur_Shuttle_Rail | Kiểu: Int
    /// </summary>
    public required string ActualRail { get; init; }

    /// <summary>
    /// Kệ thực tế
    /// Biến: Cur_Shuttle_Block | Kiểu: Int
    /// </summary>
    public required string ActualBlock { get; init; }

    /// <summary>
    /// Mã lỗi
    /// Biến: System_ErrorCode | Kiểu: Int
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Barcode của pallet char 1
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar1 { get; init; }

    /// <summary>
    /// Barcode của pallet char 2
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar2 { get; init; }

    /// <summary>
    /// Barcode của pallet char 3
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar3 { get; init; }

    /// <summary>
    /// Barcode của pallet char 4
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar4 { get; init; }

    /// <summary>
    /// Barcode của pallet char 5
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar5 { get; init; }

    /// <summary>
    /// Barcode của pallet char 6
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar6 { get; init; }

    /// <summary>
    /// Barcode của pallet char 7
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar7 { get; init; }

    /// <summary>
    /// Barcode của pallet char 8
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar8 { get; init; }

    /// <summary>
    /// Barcode của pallet char 9
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar9 { get; init; }

    /// <summary>
    /// Barcode của pallet char 10
    /// Biến: Barcode | Kiểu: Int
    /// </summary>
    public required string BarcodeChar10 { get; init; }

    #endregion
}

