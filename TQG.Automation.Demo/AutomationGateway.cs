using System.Collections.Generic;
using TQG.Automation.SDK.Abstractions;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.Demo;

public class AutomationGateway : AutomationGatewayBase
{
    public AutomationGateway() : base(DeviceProfiles)
    {
    }

    public static IReadOnlyList<DeviceProfile> DeviceProfiles => LoadDevices();

    private static List<DeviceProfile> LoadDevices() =>
    [
         new DeviceProfile
         {
            Id = "Shuttle01",
            IpAddress = "192.168.0.102",
            Cpu = PlcType.S71200,
            HasSupportInbound = true,
            Rack = 0,
            Slot = 1,
            Signals = new SignalMap
            {
                InboundCommand = "DB33.DBX0.0",
                OutboundCommand = "DB33.DBX0.1",
                TransferCommand = "DB33.DBX0.2",
                StartProcessCommand = "DB33.DBX0.3",
                CommandAcknowledged = "DB33.DBX0.4",
                CommandRejected = "DB33.DBX0.5",
                InboundComplete = "DB33.DBX0.6",
                OutboundComplete = "DB33.DBX0.7",
                TransferComplete = "DB33.DBX1.0",
                Alarm = "DB33.DBX1.1",
                InDirBlock = "DB33.DBX1.3",
                OutDirBlock ="DB33.DBX1.2",
                GateNumber ="DB33.DBW2",
                SourceFloor = "DB33.DBW50",
                SourceRail = "DB33.DBW6",
                SourceBlock = "DB33.DBW8",
                TargetFloor = "DB33.DBW10",
                TargetRail = "DB33.DBW12",
                TargetBlock = "DB33.DBW14",
                BarcodeValid = "DB33.DBX20.0",
                BarcodeInvalid = "DB33.DBX20.1",
                ActualFloor = "DB33.DBW22",
                ActualRail = "DB33.DBW24",
                ActualBlock = "DB33.DBW26",
                ErrorCode = "DB33.DBW28",
                BarcodeChar1 ="DB33.DBW30",
                BarcodeChar2 = "DB33.DBW32",
                BarcodeChar3 ="DB33.DBW34",
                BarcodeChar4 = "DB33.DBW36",
                BarcodeChar5 = "DB33.DBW38",
                BarcodeChar6 = "DB33.DBW40",
                BarcodeChar7 = "DB33.DBW42",
                BarcodeChar8 = "DB33.DBW44",
                BarcodeChar9 = "DB33.DBW46",
                BarcodeChar10 = "DB33.DBW48"
            },
            PollingIntervalSeconds = 1,
            TimeoutMinutes = 10
        }
     ];
}

