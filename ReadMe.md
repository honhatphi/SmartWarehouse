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

Ngắt kết nối với thiết bị.

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

Gửi lệnh nhập kho đến thiết bị hoặc tất cả thiết bị hỗ trợ nhập kho và đang rảnh.

- **Parameters:**
  - `taskId`: ID của nhiệm vụ nhập kho.

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `taskId` là null hoặc rỗng.

### `Task SendOutboundCommand(string taskId, Location targetLocation, short gateNumber, Direction direction)`

Gửi lệnh xuất kho đến thiết bị cụ thể, kèm theo thông tin vị trí, cửa và hướng xuất.

- **Parameters:**
  - `taskId`: ID của nhiệm vụ xuất kho.
  - `targetLocation`: Vị trí mục tiêu xuất kho.
  - `gateNumber`: Số cửa xuất kho.
  - `direction`: Hướng xuất khỏi block (block có 2 hướng vào, ví dụ: block 3).

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `taskId` là null hoặc rỗng.
  - `ArgumentNullException`: Ném ra nếu `targetLocation` là null.

### `Task SendMultipleCommands(List<TransportTask> tasks)`

Gửi danh sách lệnh xuất kho đến thiết bị cụ thể hoặc phân phối đến các thiết bị đang rảnh.

- **Parameters:**
  - `tasks`: Danh sách nhiệm vụ xuất kho (`TransportTask` chứa `taskId`, `sourceLocation` (chuyển), `targetLocation`, `gateNumber`, `inDirBlock` (chuyển), `outDirBlock`).

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `tasks` là null, rỗng hoặc chứa nhiệm vụ với `taskId` null/rỗng.
  - `ArgumentNullException`: Ném ra nếu có dữ liệu không hợp lệ (sourceLocation null khi xuất và sourceLocation, targetLocation null khi chuyển)

### `Task SendTransferCommand(string taskId, Location sourceLocation, Location targetLocation, short gateNumber, Direction inDirBlock, Direction outDirBlock)`

Gửi lệnh chuyển kho giữa hai vị trí trên thiết bị cụ thể.

- **Parameters:**
  - `taskId`: ID của nhiệm vụ chuyển kho.
  - `sourceLocation`: Vị trí nguồn của pallet.
  - `targetLocation`: Vị trí đích của pallet.
  - `gateNumber`: Số cửa xuất/nhập kho.
  - `inDirBlock`: Hướng vào block (block có 2 hướng vào, ví dụ: block 3) (Giai đoạn 1: Direction.Bottom).
  - `outDirBlock`: Hướng ra block (block có 2 hướng ra, ví dụ: block 3) (Direction.Bottom).

- **Exceptions:**
  - `ArgumentException`: Ném ra nếu `taskId` là null hoặc rỗng.
  - `ArgumentNullException`: Ném ra nếu `sourceLocation` hoặc `targetLocation` là null.

### `async Task SendValidationResult(string deviceId, string taskId, bool isValid, Location? targetLocation, Direction direction, short gateNumber)`

Gửi kết quả xác thực mã vạch đến thiết bị.

- **Parameters:**
  - `deviceId`: ID của thiết bị cần gửi kết quả.
  - `taskId`: ID của nhiệm vụ liên quan.
  - `isValid`: Kết quả xác thực mã vạch (true nếu hợp lệ).
  - `targetLocation`: Vị trí mục tiêu (tùy chọn, cần nếu hợp lệ).
  - `direction`: Hướng vào block (block có 2 hướng vào, ví dụ: block 3).
  - `gateNumber`: Số cửa nhập kho.

### `async Task<List<DeviceInfo>> GetIdleDevicesAsync()`

Lấy danh sách các thiết bị đang ở trạng thái rảnh cùng với vị trí hiện tại của chúng.

- **Returns:** Danh sách các thiết bị rảnh và vị trí hiện tại (`DeviceInfo`).

### `async Task<Location?> GetActualLocationAsync(string deviceId)`

Lấy vị trí hiện tại của thiết bị.

- **Parameters:**
  - `deviceId`: ID của thiết bị cần lấy vị trí.

- **Returns:** Vị trí hiện tại (`Location`) hoặc null nếu không thể lấy hoặc thiết bị không rảnh.

### `TransportTask[] GetPendingTask()`

Danh sách các nhiệm vụ (lệnh) đang chờ xử lý trong hàng đợi của `CommandSender`.

- **Returns:** Danh sách nhiệm vụ.

### `void RemoveTransportTasks(IEnumerable<string> taskIds)`

Loại bỏ một hoặc nhiều nhiệm vụ (lệnh) khỏi hàng đợi.

- **Parameters:**
  - `taskIds`: Danh sách ID tác vụ cần loại bỏ.

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
}
```