# AutomationGatewayBase

## Giới thiệu

Lớp `AutomationGatewayBase` là lớp cơ sở cho việc tương tác với các thiết bị PLC. Lớp này cung cấp các phương thức để quản lý thiết bị, gửi lệnh (nhập kho, xuất kho, chuyển kho), xử lý mã vạch, và giám sát trạng thái thiết bị.

**Namespace:** `TQG.Automation.SDK.Abstractions`

**Assembly:** TQG.Automation.SDK

## Constructors

### `protected AutomationGatewayBase(IEnumerable<DeviceProfile> devices)`

Khởi tạo một thể hiện của lớp `AutomationGatewayBase` với danh sách thiết bị.

- **Parameters:**
  - `devices`: Danh sách cấu hình thiết bị (không được null).

- **Exceptions:**
  - `ArgumentNullException`: Ném ra nếu danh sách `devices` là null.
  - `ArgumentException`: Ném ra nếu có ID thiết bị trùng lặp trong danh sách `devices`.

## Properties

### `IReadOnlyDictionary<string, DeviceProfile> DeviceConfigs`

Cấu hình các thiết bị được đọc chỉ (read-only).

### `Channel<BarcodeRequest> ValidationChannel`

Kênh xử lý yêu cầu mã vạch, khi phát hiện mã vạch mới sẽ gửi yêu cầu xác thực đến phần mềm.

## Events

### `EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived`

Sự kiện kích hoạt khi nhận được mã vạch từ thiết bị.

### `EventHandler<TaskSucceededEventArgs>? TaskSucceeded`

Sự kiện kích hoạt khi nhiệm vụ hoàn thành thành công.

### `EventHandler<TaskFailedEventArgs>? TaskFailed`

Sự kiện kích hoạt khi nhiệm vụ thất bại.

## Methods

### `Task ActivateDevice(string deviceId)`

Kích hoạt thiết bị.

- **Parameters:**
  - `deviceId`: ID của thiết bị cần kích hoạt.

- **Exceptions:**
  - `DeviceNotRegisteredException`: Ném ra nếu thiết bị không tồn tại.

### `void DeactivateDevice(string deviceId)`

Ngắt kết nối với thiết bị. (Bao gồm việc xóa tất cả task đang có trong hàng đợi)

- **Parameters:**
  - `deviceId`: ID của thiết bị cần hủy kích hoạt.

- **Exceptions:**
  - `DeviceNotRegisteredException`: Ném ra nếu thiết bị không tồn tại.

### `bool IsConnected(string deviceId)`

Kiểm tra trạng thái kết nối của thiết bị.

- **Parameters:**
  - `deviceId`: ID của thiết bị cần kiểm tra.

- **Returns:** `true` nếu thiết bị đang kết nối (Idle hoặc Busy), `false` nếu không.

### `DeviceStatus GetDeviceStatus(string deviceId)`

Lấy trạng thái hiện tại của thiết bị.

- **Parameters:**
  - `deviceId`: ID của thiết bị cần lấy trạng thái.

- **Returns:** Trạng thái của thiết bị (`DeviceStatus`). Nếu không có, trả về `DeviceStatus.Offline`.

### `Task SendInboundCommand(string taskId)`

Gửi lệnh nhập kho

- **Parameters:**
  - `taskId`: ID của lệnh nhập kho.

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `taskId` là null hoặc rỗng.

### `Task SendOutboundCommand(string taskId, Location targetLocation, short gateNumber, Direction direction)`

Gửi lệnh xuất kho

- **Parameters:**
  - `taskId`: ID của nhiệm vụ xuất kho.
  - `targetLocation`: Vị trí mục tiêu xuất kho.
  - `gateNumber`: Số cửa xuất kho.
  - `direction`: Hướng xuất khỏi block (block có 2 hướng vào, ví dụ: block 3).

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `taskId` là null hoặc rỗng.
  - `ArgumentNullException`: Ném ra nếu `targetLocation` là null.

### `Task SendMultipleCommands(List<TransportTask> tasks)`

 Gửi danh sách bao gồm nhiều lệnh.

- **Parameters:**
  - `tasks`: Danh sách lệnh.

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `tasks` là null, rỗng hoặc chứa nhiệm vụ với `taskId` null/rỗng.
  - `ArgumentNullException`: Ném ra nếu có dữ liệu không hợp lệ (sourceLocation null khi xuất và sourceLocation, targetLocation null khi chuyển)

### `Task SendTransferCommand(string taskId, Location sourceLocation, Location targetLocation, short gateNumber, Direction inDirBlock, Direction outDirBlock)`

Gửi lệnh chuyển vị trí
- **Parameters:**
  - `taskId`: ID của lệnh chuyển kho.
  - `sourceLocation`: Vị trí nguồn của pallet.
  - `targetLocation`: Vị trí đích của pallet.
  - `gateNumber`: Số cửa xuất/nhập kho.
  - `inDirBlock`: Hướng vào block (block có 2 hướng vào, ví dụ: block 3) (Giai đoạn 1: Direction.Bottom).
  - `outDirBlock`: Hướng ra block (block có 2 hướng ra, ví dụ: block 3) (Direction.Bottom).

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `taskId` là null hoặc rỗng.
  - `ArgumentNullException`: Ném ra nếu `sourceLocation` hoặc `targetLocation` là null.

### `async Task SendValidationResult(string deviceId, string taskId, bool isValid, Location? targetLocation, Direction direction, short gateNumber)`

Gửi kết quả xác thực mã vạch đến thiết bị. (Dùng để phản hồi lại event BarcodeReceived)

- **Parameters:**
  - `deviceId`: ID của thiết bị cần gửi kết quả.
  - `taskId`: ID của nhiệm vụ liên quan.
  - `isValid`: Kết quả xác thực mã vạch (true nếu hợp lệ).
  - `targetLocation`: Vị trí mục tiêu (tùy chọn, cần nếu hợp lệ).
  - `direction`: Hướng vào block (block có 2 hướng vào, ví dụ: block 3).
  - `gateNumber`: Số cửa nhập kho.

### `async Task<List<DeviceInfo>> GetIdleDevicesAsync()`

Lấy danh sách các shuttle đang ở trạng thái rảnh cùng với vị trí hiện tại của chúng.

- **Returns:** Danh sách các shuttle rảnh và vị trí hiện tại.

### `async Task<Location?> GetActualLocationAsync(string deviceId)`

Lấy vị trí hiện tại của shuttle.

- **Parameters:**
  - `deviceId`: ID của shuttle cần lấy vị trí.

- **Returns:** Vị trí hiện tại (`Location`) hoặc null nếu shuttle đang không hoạt động.

### `TransportTask[] GetPendingTask()`

Danh sách các lệnh đang chờ xử lý trong hàng đợi.

- **Returns:** Danh sách lệnh.

### `bool RemoveTransportTasks(IEnumerable<string> taskIds)`

Loại bỏ một hoặc nhiều khỏi hàng đợi.

- **Parameters:**
  - `taskIds`: Danh sách ID lệnh cần loại bỏ.

- **Returns:** False nếu list trống hoặc chưa dừng (IsPauseQueue = False), True nếu return thành công.

### `string? GetCurrentTask(string deviceId)`
Lấy TaskId đang thực hiện

- **Parameters:**
  - `deviceId`: Id của shuttle.

- **Returns:** TaskId hoặc null.

### `void PauseQueue()`
Tạm dừng chạy lệnh (các lệnh đang chạy vẫn tiếp tục đến khi hoàn thành)

### `void ResumeQueue()`
Tiếp tục chạy lệnh có trong hàng chờ

### `IsPauseQueue`
Kiểm tra queue có đang đóng không

### `void Dispose()`

Giải phóng tài nguyên của lớp, bao gồm dispose các thành phần con.

## Phương thức Private

### `private static void ValidateUniqueDeviceIds(IEnumerable<DeviceProfile> devices)`

Kiểm tra tính duy nhất của ID thiết bị trong danh sách cấu hình.

- **Parameters:**
  - `devices`: Danh sách cấu hình thiết bị.

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu có ID trùng lặp.

## Object Definitions

### `DeviceProfile`
Cấu hình thiết bị PLC.  
- **Properties:**
  - `Id`: Định danh duy nhất (e.g., "SHUTTLE_01").
  - `IpAddress`: Địa chỉ IP của PLC.
  - `Cpu`: Loại CPU của PLC (mặc định: S7-1200).
  - `Rack`: Thông số PLC (mặc định: 0).
  - `Slot`: Thông số PLC (mặc định: 1).
  - `Signals`: Địa chỉ các thanh ghi (`SignalMap`).
  - `HasSupportInbound`: Hỗ trợ lệnh nhập kho.
  - `PollingIntervalSeconds`: Khoảng thời gian polling trạng thái (mặc định: 1 giây).
  - `TimeoutMinutes`: Timeout cho lệnh (mặc định: 10 phút).

### `BarcodeReceivedEventArgs`
Sự kiện khi nhận mã vạch từ thiết bị PLC.  
- **Properties:**
  - `DeviceId`: ID thiết bị PLC.
  - `TaskId`: ID nhiệm vụ đang xử lý.
  - `Barcode`: Mã vạch (10 ký tự). 
  Nếu chưa đủ 10 kí tự thì sẽ thay là 0 ví dụ: Pallet `1234` thì barcode nhận là `0000001234`

### `TaskFailedEventArgs`
Sự kiện khi nhiệm vụ thất bại.  
- **Properties:**
  - `DeviceId`: ID thiết bị PLC.
  - `TaskId`: ID nhiệm vụ thất bại.
  - `ErrorDetail`: Chi tiết lỗi (`ErrorDetail`).

### `ErrorDetail`
Thông tin chi tiết về lỗi.  
- **Properties:**
  - `ErrorCode`: Mã lỗi.
  - `ErrorMessage`: Mô tả lỗi.

### `TaskSucceededEventArgs`
Sự kiện khi nhiệm vụ hoàn thành thành công.  
- **Properties:**
  - `DeviceId`: ID thiết bị PLC.
  - `TaskId`: ID nhiệm vụ thành công.

### `DeviceInfo`
Thông tin thiết bị và vị trí hiện tại.  
- **Properties:**
  - `DeviceId`: ID thiết bị.
  - `Location`: Vị trí hiện tại (`Location`).

### `Location`
Vị trí trong kho (tầng, dãy, kệ).  
- **Properties:**
  - `Floor`: Tầng (short) (1 => 7).
  - `Rail`: Dãy (short) (1 => 24 dãy).
  - `Block`: Kệ (short) (GĐ1: Bao gồm 3 và 5).

### `Direction`
Hướng di chuyển của shuttle vào block (Top hoặc Bottom).
Áp dụng cho block 3 (GĐ1 thì tất cả giá trị đều là Bottom)
- **Type:** Enum (giá trị: `Top`, `Bottom`).

### `TransportTask`
Lệnh di chuyển pallet.  
- **Properties:**
  - `TaskId`: ID lệnh.
  - `CommandType`: Loại lệnh (`Inbound`, `Outbound`, `Transfer`).
  - `SourceLocation`: Vị trí nguồn (`Location`, dùng cho `Outbound` và `Transfer`).
  - `TargetLocation`: Vị trí đích (`Location`, dùng cho `Transfer`).
  - `GateNumber`: Số cửa xuất/nhập (short).
  - `InDirBlock`: Hướng vào block (`Direction`, dùng cho `Transfer`).
  - `OutDirBlock`: Hướng ra block (`Direction`, dùng cho `Outbound` và `Transfer`).

### `SignalMap`
Địa chỉ các thanh ghi PLC.  
- **Command Properties:**
  - `InboundCommand`: Lệnh nhập.
  - `OutboundCommand`: Lệnh xuất.
  - `TransferCommand`: Lệnh chuyển.
  - `StartProcessCommand`: Bắt đầu quá trình.
- **Status Properties:**
  - `CommandAcknowledged`: Lệnh được xác nhận.
  - `CommandRejected`: Lệnh bị từ chối.
  - `InboundComplete`: Hoàn thành nhập.
  - `OutboundComplete`: Hoàn thành xuất.
  - `TransferComplete`: Hoàn thành chuyển.
  - `Alarm`: Lỗi khi thực hiện.
- **Gate & Direction Properties:**
  - `OutDirBlock`: Hướng xuất.
  - `InDirBlock`: Hướng nhập.
  - `GateNumber`: Cửa xuất/nhập.
- **Source Location Properties:**
  - `SourceFloor`: Tầng nguồn.
  - `SourceRail`: Dãy nguồn.
  - `SourceBlock`: Kệ nguồn.
- **Target Location Properties:**
  - `TargetFloor`: Tầng đích.
  - `TargetRail`: Dãy đích.
  - `TargetBlock`: Kệ đích.
- **Feedback Properties:**
  - `BarcodeValid`: Mã vạch hợp lệ.
  - `BarcodeInvalid`: Mã vạch không hợp lệ.
  - `ActualFloor`: Tầng thực tế.
  - `ActualRail`: Dãy thực tế.
  - `ActualBlock`: Kệ thực tế.
  - `ErrorCode`: Mã lỗi.
  - `BarcodeChar1` to `BarcodeChar10`: Ký tự mã vạch (`Barcode`, Int).

### `Khác`
- `DeviceId`: Chính là Id trong DeviceProfile được cấu hình lúc khởi tạo AutomationGatewayBase
- `TaskId`: Sẽ tương ứng với các lệnh nhập - xuất - chuyển trong kho với tỉ lệ 1 pallet là 1 lệnh.

## Lưu ý

- Lớp này là abstract, vì vậy cần kế thừa để sử dụng.
- Các lệnh gửi (SendInboundCommand, SendOutboundCommand, v.v.) dựa vào `CommandSender` nội bộ để xử lý hàng đợi và phân phối nhiệm vụ.
- Xử lý mã vạch sử dụng `BarcodeHandler` và kênh validation.
- Đảm bảo xử lý các ngoại lệ như `DeviceNotRegisteredException`, `PlcConnectionFailedException` khi sử dụng các phương thức liên quan đến thiết bị.

## Ví dụ Sử dụng

### AutomationGateway
```csharp
public class AutomationGateway : AutomationGatewayBase
{
    public AutomationGateway() : base(DeviceProfiles) { }

    // Có thể lấy từ dữ liệu cấu hình hoặc từ một nguồn khác
    public static IReadOnlyList<DeviceProfile> DeviceProfiles => LoadDevices();

    private static List<DeviceProfile> LoadDevices() => [];
}
```
### GatewayBackgroundService
```csharp
public class GatewayBackgroundService : BackgroundService
{
    private readonly AutomationGateway _gateway;

    public GatewayBackgroundService(AutomationGateway gateway)
    {
        _gateway = gateway;

        // Đăng ký các sự kiện từ AutomationGateway
        _gateway.BarcodeReceived += OnBarcodeReceived;
        _gateway.TaskSucceeded += OnTaskSucceeded;
        _gateway.TaskFailed += OnTaskFailed;
    }

    private void OnBarcodeReceived(object? sender, BarcodeReceivedEventArgs e)
    {
        // TODO: Thêm logic xử lý mã vạch, ví dụ: xác thực mã vạch, lấy vị trí đích, cửa và hướng nhập
    }

    private void OnTaskSucceeded(object? sender, TaskSucceededEventArgs e)
    {
        // TODO: Thêm logic cập nhật cơ sở dữ liệu, ghi log hoặc thông báo
    }

    private void OnTaskFailed(object? sender, TaskFailedEventArgs e)
    {
        // TODO: Thêm logic cập nhật cơ sở dữ liệu, ghi log hoặc thông báo
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        // Hủy đăng ký các sự kiện khi dịch vụ dừng
        _gateway.BarcodeReceived -= OnBarcodeReceived;
        _gateway.TaskSucceeded -= OnTaskSucceeded;
        _gateway.TaskFailed -= OnTaskFailed;

        _gateway.Dispose();
    }

    public Task SendInboundCommandAsync(string taskId)
        => _gateway.SendInboundCommand(taskId);

    public Task SendMultipleCommands(List<TransportTask> tasks)
        => _gateway.SendMultipleCommands(tasks);

    public Task SendOutboundCommandAsync(
        string taskId,
        Location targetLocation,
        short gate,
        Direction direction)
        => _gateway.SendOutboundCommand(taskId, targetLocation, gate, direction);

    public Task SendTransferCommandAsync(
        string taskId,
        Location sourceLocation,
        Location targetLocation,
        short gateNumber,
        Direction inDirectionBlock,
        Direction outDirectionBlock)
        => _gateway.SendTransferCommand(taskId, sourceLocation, targetLocation, gateNumber, inDirectionBlock, outDirectionBlock);

    public Task ActivateDevice(string deviceId)
        => _gateway.ActivateDevice(deviceId);

    public void DeactivateDevice(string deviceId)
        => _gateway.DeactivateDevice(deviceId);

    public bool IsConnected(string deviceId)
        => _gateway.IsConnected(deviceId);

    public async Task<Location?> GetActualLocationAsync(string deviceId)
        => await _gateway.GetActualLocationAsync(deviceId);

    public DeviceStatus GetDeviceStatus(string deviceId)
        => _gateway.GetDeviceStatus(deviceId);

    public async Task<List<DeviceInfo>> GetIdleDevicesAsync()
        => await _gateway.GetIdleDevicesAsync();

    public TransportTask[] GetPendingTask() => _gateway.GetPendingTask();

    public void RemoveTransportTasks(IEnumerable<string> taskIds);

    public void PauseQueue() => _gateway.PauseQueue();

    public void ResumeQueue() => _gateway.ResumeQueue();

    public bool IsPauseQueue => _gateway.IsPauseQueue;

    public string? GetCurrentTask(string deviceId) => _gateway.GetCurrentTask(deviceId);
}
```