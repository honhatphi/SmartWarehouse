using System.ComponentModel;

namespace TQG.Automation.SDK.Shared;

public enum CommandType
{
    [Description("Nhập")]
    Inbound,

    [Description("Xuất")]
    Outbound,

    [Description("Chuyển")]
    Transfer
}

public enum PlcType
{
    S7200 = 0,

    Logo0BA8 = 1,

    S7200Smart = 2,

    S7300 = 10,

    S7400 = 20,

    S71200 = 30,

    S71500 = 40,
}

public enum Direction
{
    [Description("Từ dưới lên")]
    Bottom = 0,

    [Description("Từ trên xuống")]
    Top = 1
}

public enum DeviceStatus
{
    [Description("Ngoại tuyến")]
    Offline,

    [Description("Sẵn sàng")]
    Idle,

    [Description("Đang thực thi")]
    Busy,

    [Description("Đang gặp lỗi")]
    Error,

    [Description("Đang sạc pin")]
    Charging
}

