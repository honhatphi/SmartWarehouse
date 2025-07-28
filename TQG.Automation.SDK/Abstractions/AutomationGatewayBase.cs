using System.Threading.Channels;
using TQG.Automation.SDK.Events;
using TQG.Automation.SDK.Internal;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Abstractions;

/// <summary>
/// Lớp cơ sở cho tương tác với các thiết bị PLC.
/// </summary>
public abstract class AutomationGatewayBase : IDisposable
{
    private readonly DeviceMonitor _deviceMonitor;
    private readonly BarcodeHandler _barcodeHandler;
    private readonly CommandSender _commandSender;

    // Kênh xử lý yêu cầu mã vạch, được sử dụng để hàng đợi validation.
    protected readonly Channel<BarcodeRequest> ValidationChannel;

    /// <summary>
    /// Cấu hình các thiết bị
    /// </summary>
    public IReadOnlyDictionary<string, DeviceProfile> DeviceConfigs { get; }

    /// <summary>
    /// Sự kiện kích hoạt khi nhận được mã vạch từ thiết bị.
    /// </summary>
    public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;

    /// <summary>
    /// Sự kiện kích hoạt khi lệnh hoàn thành thành công.
    /// </summary>
    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;

    /// <summary>
    /// Sự kiện kích hoạt khi lệnh thất bại.
    /// </summary>
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    /// <summary>
    /// Khởi tạo một thể hiện của lớp AutomationGatewayBase.
    /// </summary>
    /// <param name="devices">Danh sách cấu hình thiết bị (không được null).</param>
    /// <exception cref="ArgumentNullException">Ném ra nếu danh sách devices là null.</exception>
    /// <exception cref="ArgumentException">Ném ra nếu có ID thiết bị trùng lặp trong danh sách devices.</exception>
    protected AutomationGatewayBase(IEnumerable<DeviceProfile> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ValidateUniqueDeviceIds(devices);

        DeviceConfigs = devices.ToDictionary(d => d.Id);

        int inboundCount = devices.Count(d => d.HasSupportInbound);
        int capacity = inboundCount * 2;
        ValidationChannel = Channel.CreateBounded<BarcodeRequest>(capacity > 0 ? capacity : 1);

        _deviceMonitor = new DeviceMonitor(DeviceConfigs);
        _barcodeHandler = new BarcodeHandler(_deviceMonitor, ValidationChannel);
        _commandSender = new CommandSender(_deviceMonitor, _barcodeHandler);

        _barcodeHandler.BarcodeReceived += (sender, args) => BarcodeReceived?.Invoke(this, args);
        _barcodeHandler.TaskFailed += (sender, args) => TaskFailed?.Invoke(this, args);
        _commandSender.TaskSucceeded += (sender, args) => TaskSucceeded?.Invoke(this, args);
        _commandSender.TaskFailed += (sender, args) => TaskFailed?.Invoke(this, args);
    }

    /// <summary>
    /// Kích hoạt thiết bị và bắt đầu giám sát trạng thái của nó.
    /// </summary>
    /// <param name="deviceId">ID của thiết bị cần kích hoạt.</param>
    /// <exception cref="DeviceNotRegisteredException">Ném ra nếu thiết bị không tồn tại.</exception>
    public Task ActivateDevice(string deviceId) => _deviceMonitor.StartMonitoring(deviceId);

    /// <summary>
    /// Hủy kích hoạt thiết bị và ngắt kết nối với nó.
    /// </summary>
    /// <param name="deviceId">ID của thiết bị cần hủy kích hoạt.</param>
    /// <exception cref="DeviceNotRegisteredException">Ném ra nếu thiết bị không tồn tại.</exception>
    public void DeactivateDevice(string deviceId)
    {
        _deviceMonitor.StopMonitoring(deviceId);

        Dispose();
    }

    /// <summary>
    /// Kiểm tra trạng thái kết nối của thiết bị.
    /// </summary>
    /// <param name="deviceId">ID của thiết bị cần kiểm tra.</param>
    /// <returns>True nếu thiết bị đang kết nối (Idle hoặc Busy), False nếu không.</returns>
    public bool IsConnected(string deviceId) => _deviceMonitor.IsConnected(deviceId);

    /// <summary>
    /// Lấy trạng thái hiện tại của thiết bị.
    /// </summary>
    /// <param name="deviceId">ID của thiết bị cần lấy trạng thái.</param>
    /// <returns>Trạng thái của thiết bị (DeviceStatus). Nếu không có, trả về DeviceStatus.Offline.</returns>
    public DeviceStatus GetDeviceStatus(string deviceId) => _deviceMonitor.GetDeviceStatus(deviceId);

    /// <summary>
    /// Gửi lệnh nhập kho
    /// </summary>
    /// <param name="taskId">ID của lệnh nhập kho.</param>
    /// <exception cref="ArgumentException">Ném ra nếu taskId là null hoặc rỗng.</exception>
    public Task SendInboundCommand(string taskId)
        => _commandSender.SendInboundCommand(taskId);

    /// <summary>
    /// Gửi lệnh xuất kho
    /// </summary>
    /// <param name="taskId">ID của lệnh xuất kho.</param>
    /// <param name="targetLocation">Vị trí mục tiêu xuất kho.</param>
    /// <param name="gateNumber">Số cửa xuất kho.</param>
    /// <param name="direction">Hướng xuất khỏi block (block có 2 hướng vào vd: block 3).</param>
    /// <exception cref="ArgumentException">Ném ra nếu deviceId hoặc taskId là null hoặc rỗng.</exception>
    /// <exception cref="ArgumentNullException">Ném ra nếu targetLocation là null.</exception>
    public Task SendOutboundCommand(string taskId, Location targetLocation, short gateNumber, Direction direction)
        => _commandSender.SendOutboundCommand(taskId, targetLocation, gateNumber, direction);

    /// <summary>
    /// Gửi danh sách bao gồm nhiều lệnh.
    /// </summary>
    /// <param name="tasks">Danh sách lệnh di chuyển.</param>
    /// <exception cref="ArgumentException">Ném ra nếu tasks là null, rỗng hoặc chứa lệnh với taskId null/rỗng.</exception>
    /// <exception cref="ArgumentNullException">Ném ra nếu bất kỳ lệnh nào có targetLocation null.</exception>
    public Task SendMultipleCommands(List<TransportTask> tasks)
        => _commandSender.SendMultipleCommands(tasks);

    /// <summary>
    /// Gửi lệnh chuyển vị trí
    /// </summary>
    /// <param name="taskId">ID của lệnh chuyển vị trí.</param>
    /// <param name="sourceLocation">Vị trí nguồn của hàng hóa.</param>
    /// <param name="targetLocation">Vị trí đích của hàng hóa.</param>
    /// <param name="gateNumber">Số cửa xuất/nhập kho.</param>
    /// <param name="inDirBlock">Hướng vào block (block có 2 hướng vào vd: block 3).</param>
    /// <param name="outDirBlock">Hướng ra block (block có 2 hướng ra vd: block 3).</param>
    /// <exception cref="ArgumentException">Ném ra nếu deviceId hoặc taskId là null hoặc rỗng.</exception>
    /// <exception cref="ArgumentNullException">Ném ra nếu sourceLocation hoặc targetLocation là null.</exception>
    public Task SendTransferCommand(
        string taskId,
        Location sourceLocation,
        Location targetLocation,
        short gateNumber,
        Direction inDirBlock,
        Direction outDirBlock)
        => _commandSender.SendTransferCommand(taskId, sourceLocation, targetLocation, gateNumber, inDirBlock, outDirBlock);

    /// <summary>
    /// Gửi kết quả xác thực mã vạch đến thiết bị.
    /// </summary>
    /// <param name="deviceId">ID của thiết bị cần gửi kết quả.</param>
    /// <param name="taskId">ID của lệnh liên quan.</param>
    /// <param name="isValid">Kết quả xác thực mã vạch (true nếu hợp lệ).</param>
    /// <param name="targetLocation">Vị trí mục tiêu (tùy chọn, cần nếu hợp lệ).</param>
    /// <param name="direction">Hướng vào block (block có 2 hướng vào vd: block 3).</param>
    /// <param name="gateNumber">Số cửa nhập kho.</param>
    public async Task SendValidationResult(
        string deviceId,
        string taskId,
        bool isValid,
        Location? targetLocation,
        Direction direction,
        short gateNumber)
        => await _barcodeHandler.SendValidationResult(deviceId, taskId, isValid, targetLocation, direction, gateNumber);

    /// <summary>
    /// Lấy danh sách các shuttle đang ở trạng thái rảnh cùng với vị trí hiện tại của chúng.
    /// </summary>
    /// <returns>Danh sách các shuttle rảnh và vị trí hiện tại.</returns>
    public async Task<List<DeviceInfo>> GetIdleDevicesAsync() => await _deviceMonitor.GetIdleDevicesAsync();

    /// <summary>
    /// Lấy vị trí hiện tại của shuttle.
    /// </summary>
    /// <param name="deviceId">ID của shuttle cần lấy vị trí.</param>
    /// <returns>Vị trí hiện tại (Location) hoặc null nếu shuttle đang không hoạt động.</returns>
    public async Task<Location?> GetActualLocationAsync(string deviceId) => await _barcodeHandler.GetActualLocationAsync(deviceId);

    /// <summary>
    /// Danh sách các lệnh đang chờ xử lý trong hàng đợi.
    /// </summary>
    /// <returns>Danh sách lệnh</returns>
    public TransportTask[] GetPendingTask() => _commandSender.GetQueuedTasks();

    /// <summary>
    /// Loại bỏ một hoặc nhiều lệnh khỏi hàng đợi.
    /// </summary>
    /// <param name="taskIds">Danh sách lệnh</param>
    /// <returns>False nếu list trống hoặc chưa dừng (IsPauseQueue = False), True nếu return thành công.</returns>
    public bool RemoveTransportTasks(IEnumerable<string> taskIds)
        => _commandSender.RemoveTasks(taskIds);

    /// <summary>
    /// Lấy TaskId đang thực hiện
    /// </summary>
    /// <param name="deviceId">Id của shuttle.</param>
    /// <returns>TaskId hoặc null</returns>
    public string? GetCurrentTask(string deviceId) => _commandSender.GetCurrentTask(deviceId);


    /// <summary>
    /// Tạm dừng chạy lệnh (các lệnh đang chạy vẫn tiếp tục đến khi hoàn thành)
    /// </summary>
    public void PauseQueue()
        => _commandSender.Pause();

    /// <summary>
    /// Tiếp tục chạy lệnh có trong hàng chờ
    /// </summary>
    public void ResumeQueue()
        => _commandSender.Resume();

    /// <summary>
    /// Kiểm tra queue có đang tạm dừng không
    /// </summary>
    public bool IsPauseQueue
        => _commandSender.IsPauseQueue;

    /// <summary>
    /// Giải phóng tài nguyên của lớp, bao gồm dispose các thành phần con.
    /// </summary>
    public void Dispose()
    {
        _commandSender.Dispose();
        _barcodeHandler.Dispose();
        _deviceMonitor.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Kiểm tra tính duy nhất của ID thiết bị trong danh sách cấu hình.
    /// </summary>
    /// <param name="devices">Danh sách cấu hình thiết bị.</param>
    /// <exception cref="ArgumentException">Ném ra nếu có ID trùng lặp.</exception>
    private static void ValidateUniqueDeviceIds(IEnumerable<DeviceProfile> devices)
    {
        var duplicateKeys = devices.GroupBy(d => d.Id).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicateKeys.Any())
        {
            throw new ArgumentException($"Duplicate device IDs: {string.Join(", ", duplicateKeys)}");
        }
    }
}