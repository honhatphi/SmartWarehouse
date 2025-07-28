using System.Collections.Concurrent;
using TQG.Automation.SDK.Communication;
using TQG.Automation.SDK.Events;
using TQG.Automation.SDK.Exceptions;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Internal;

internal sealed class CommandSender : IDisposable
{
    private readonly DeviceMonitor _deviceMonitor;
    private readonly BarcodeHandler _barcodeHandler;
    private readonly ConcurrentDictionary<string, (Task PollTask, CancellationTokenSource Cts)> activePollTasks = new();
    private readonly TaskDispatcher _taskDispatcher;

    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    public CommandSender(
        DeviceMonitor monitor,
        BarcodeHandler barcodeHandler)
    {
        _deviceMonitor = monitor;
        _barcodeHandler = barcodeHandler;
        _taskDispatcher = new TaskDispatcher(_deviceMonitor.DeviceProfile, this);

        monitor.DeviceStatusChanged += _taskDispatcher.OnDeviceStatusChanged;
    }

    public async Task SendInboundCommand(string taskId)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);

        TransportTask inboundTask = new()
        {
            TaskId = taskId,
            CommandType = CommandType.Inbound
        };

        _taskDispatcher.EnqueueTasks([inboundTask]);
        await _taskDispatcher.ProcessQueueIfNeeded();
    }

    public async Task SendOutboundCommand(string taskId, Location sourceLocation, short gateNumber, Direction direction)
    {
        ValidateOutboundArguments(taskId, sourceLocation);

        var transportTask = new TransportTask
        {
            TaskId = taskId,
            CommandType = CommandType.Outbound,
            SourceLocation = sourceLocation,
            GateNumber = gateNumber,
            OutDirBlock = direction
        };

        _taskDispatcher.EnqueueTasks([transportTask]);
        await _taskDispatcher.ProcessQueueIfNeeded();

    }

    public async Task SendTransferCommand(
        string taskId,
        Location sourceLocation,
        Location targetLocation,
        short gateNumber,
        Direction inDirBlock,
        Direction outDirBlock)
    {
        ValidateTransferArguments(taskId, sourceLocation, targetLocation);

        var transportTask = new TransportTask
        {
            TaskId = taskId,
            CommandType = CommandType.Transfer,
            SourceLocation = sourceLocation,
            TargetLocation = targetLocation,
            GateNumber = gateNumber,
            InDirBlock = inDirBlock,
            OutDirBlock = outDirBlock
        };

        _taskDispatcher.EnqueueTasks([transportTask]);

        await _taskDispatcher.ProcessQueueIfNeeded();
    }

    public async Task SendMultipleCommands(List<TransportTask> tasks)
    {
        if (tasks == null || tasks.Count == 0)
        {
            throw new ArgumentException("Tasks must not be null or empty.");
        }

        foreach (var task in tasks)
        {
            if (task.CommandType == CommandType.Outbound)
            {
                ValidateOutboundArguments(task.TaskId, task.SourceLocation);
            }
            else if (task.CommandType == CommandType.Transfer)
            {
                ValidateTransferArguments(task.TaskId, task.SourceLocation!, task.TargetLocation!);
            }
        }

        _taskDispatcher.EnqueueTasks(tasks);

        await _taskDispatcher.ProcessQueueIfNeeded();
    }


    private static async Task TriggerInboundCommand(PlcConnector connector, SignalMap signals)
    {
        await connector.WriteAsync(signals.InboundCommand, true);
        await connector.WriteAsync(signals.StartProcessCommand, true);
    }

    private static async Task TriggerOutboundCommand(PlcConnector connector, SignalMap signals, Location targetLocation, short gateNumber, bool outDirBlock)
    {
        await connector.WriteAsync(signals.OutboundCommand, true);
        await connector.WriteAsync(signals.StartProcessCommand, true);

        await connector.WriteAsync(signals.SourceFloor, targetLocation.Floor);
        await connector.WriteAsync(signals.SourceRail, targetLocation.Rail);
        await connector.WriteAsync(signals.SourceBlock, targetLocation.Block);
        await connector.WriteAsync(signals.GateNumber, gateNumber);
        await connector.WriteAsync(signals.OutDirBlock, outDirBlock);
    }

    private static async Task TriggerTransferCommand(PlcConnector connector, SignalMap signals, Location sourceLocation, Location targetLocation, short gateNumber, bool inDirValue, bool outDirValue)
    {
        await connector.WriteAsync(signals.TransferCommand, true);
        await connector.WriteAsync(signals.StartProcessCommand, true);

        await connector.WriteAsync(signals.SourceFloor, sourceLocation.Floor);
        await connector.WriteAsync(signals.SourceRail, sourceLocation.Rail);
        await connector.WriteAsync(signals.SourceBlock, sourceLocation.Block);

        await connector.WriteAsync(signals.TargetFloor, targetLocation.Floor);
        await connector.WriteAsync(signals.TargetRail, targetLocation.Rail);
        await connector.WriteAsync(signals.TargetBlock, targetLocation.Block);

        await connector.WriteAsync(signals.GateNumber, gateNumber);
        await connector.WriteAsync(signals.InDirBlock, inDirValue);
        await connector.WriteAsync(signals.OutDirBlock, outDirValue);
    }

    private Task StartInboundPolling(string deviceId, PlcConnector connector, string taskId, CancellationToken token)
        => Task.Run(async () =>
        {
            DeviceProfile profile = _deviceMonitor.GetProfile(deviceId);
            var signals = profile.Signals;
            bool barcodeProcessed = false;
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(profile.PollingIntervalSeconds));
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(profile.TimeoutMinutes);
            string defaultBarcode = "0000000000";
            while ((DateTime.UtcNow - startTime) < timeout && !token.IsCancellationRequested)
            {
                if (!await timer.WaitForNextTickAsync(token)) break;

                try
                {
                    if (!barcodeProcessed)
                    {
                        string barcode = await BarcodeHandler.ReadBarcodeAsync(connector, signals);

                        if (string.IsNullOrEmpty(barcode) || barcode == defaultBarcode)
                        {
                            continue;
                        }

                        await _barcodeHandler.SendBarcodeAsync(deviceId, taskId, barcode);
                        barcodeProcessed = true;

                        if (await HandleCommandStatusAsync(connector, signals, deviceId, taskId))
                        {
                            break;
                        }
                    }

                    if (await CheckInboundCompletionAsync(connector, signals, deviceId, taskId))
                    {
                        break;
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    HandlePollingException(deviceId, taskId, ex, nameof(CommandType.Inbound));
                    break;
                }
            }

            activePollTasks.TryRemove(taskId, out _);
        }, token);

    private Task StartOutboundPolling(string deviceId, string taskId, PlcConnector connector, SignalMap signals, int timeoutMinutes, CancellationToken token)
        => Task.Run(async () =>
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(timeoutMinutes);

            while ((DateTime.UtcNow - startTime) < timeout && !token.IsCancellationRequested)
            {
                if (!await timer.WaitForNextTickAsync(token)) break;

                try
                {
                    if (await HandleCommandStatusAsync(connector, signals, deviceId, taskId))
                    {
                        break;
                    }

                    if (await CheckOutboundCompletionAsync(connector, signals, deviceId, taskId))
                    {
                        break;
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    HandlePollingException(deviceId, taskId, ex, nameof(CommandType.Outbound));
                    break;
                }
            }

            activePollTasks.TryRemove(taskId, out _);
        }, token);

    private Task StartTransferPolling(string deviceId, string taskId, PlcConnector connector, SignalMap signals, int timeoutMinutes, CancellationToken token)
        => Task.Run(async () =>
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(timeoutMinutes);

            while ((DateTime.UtcNow - startTime) < timeout && !token.IsCancellationRequested)
            {
                if (!await timer.WaitForNextTickAsync(token)) break;

                try
                {
                    if (await HandleCommandStatusAsync(connector, signals, deviceId, taskId))
                    {
                        break;
                    }

                    if (await CheckTransferCompletionAsync(connector, signals, deviceId, taskId))
                    {
                        break;
                    }
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    HandlePollingException(deviceId, taskId, ex, nameof(CommandType.Transfer));
                    break;
                }
            }

            activePollTasks.TryRemove(taskId, out _);
        }, token);

    private async Task<bool> HandleCommandStatusAsync(PlcConnector connector, SignalMap signals, string deviceId, string taskId)
    {
        bool acknowledged = await connector.ReadAsync<bool>(signals.CommandAcknowledged);
        bool rejected = await connector.ReadAsync<bool>(signals.CommandRejected);

        if (rejected)
        {
            short errorCode = await connector.ReadAsync<short>(signals.ErrorCode);

            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.CommandReject(errorCode)));

            _deviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Error);

            return true;
        }

        if (!acknowledged)
        {
            return false;
        }

        return false;
    }

    private async Task<bool> CheckInboundCompletionAsync(PlcConnector connector, SignalMap signals, string deviceId, string taskId)
        => await CheckCompletionAsync(connector, signals, deviceId, taskId, signals.InboundComplete);

    private async Task<bool> CheckOutboundCompletionAsync(PlcConnector connector, SignalMap signals, string deviceId, string taskId)
        => await CheckCompletionAsync(connector, signals, deviceId, taskId, signals.OutboundComplete);

    private async Task<bool> CheckTransferCompletionAsync(PlcConnector connector, SignalMap signals, string deviceId, string taskId)
        => await CheckCompletionAsync(connector, signals, deviceId, taskId, signals.TransferComplete);

    private async Task<bool> CheckCompletionAsync(PlcConnector connector, SignalMap signals, string deviceId, string taskId, string completeSignal)
    {
        bool alarm = await connector.ReadAsync<bool>(signals.Alarm);
        bool complete = await connector.ReadAsync<bool>(completeSignal);

        if (complete || alarm)
        {
            short errorCode = await connector.ReadAsync<short>(signals.ErrorCode);

            if (complete && !alarm)
            {
                TaskSucceeded?.Invoke(this, new TaskSucceededEventArgs(deviceId, taskId));
                _deviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Idle);
                await Task.Delay(5000);
            }
            else
            {
                TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.RunningFailure(taskId, deviceId, errorCode)));
                _deviceMonitor.UpdateDeviceStatus(deviceId, DeviceStatus.Error);
            }

            return true;
        }

        return false;
    }

    private void HandlePollingException(string deviceId, string taskId, Exception ex, string pollType)
    {
        TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.PollingException(pollType, deviceId, taskId, ex)));
    }

    private static void ValidateOutboundArguments(string taskId, Location? sourceLocation)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        ArgumentNullException.ThrowIfNull(sourceLocation);
    }

    private static void ValidateTransferArguments(string taskId, Location sourceLocation, Location targetLocation)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskId);
        ArgumentNullException.ThrowIfNull(sourceLocation);
        ArgumentNullException.ThrowIfNull(targetLocation);
    }

    public void Dispose()
    {
        foreach (var (_, Cts) in activePollTasks.Values)
        {
            Cts.Cancel();
            Cts.Dispose();
        }

        activePollTasks.Clear();
        _taskDispatcher.Dispose();
    }

    public TransportTask[] GetQueuedTasks() => _taskDispatcher.GetQueuedTasks();

    public bool RemoveTasks(IEnumerable<string> taskIds) => _taskDispatcher.RemoveTasks(taskIds);

    public string? GetCurrentTask(string deviceId) => _taskDispatcher.GetCurrentTask(deviceId);

    public void Pause() => _taskDispatcher.Pause();

    public void Resume() => _taskDispatcher.Resume();

    public bool IsPauseQueue => _taskDispatcher.IsPaused;

    private class TaskDispatcher : IDisposable
    {
        private readonly IReadOnlyDictionary<string, DeviceProfile> _deviceProfile;
        private readonly CommandSender _sender;

        private readonly ConcurrentQueue<TransportTask> _taskQueue = new();
        private readonly ConcurrentDictionary<string, string> _assigningDevices = new();
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _cts = new();
        private static readonly Location _gateLocation = new(1, 14, 5);  // Fixed gate location for Inbound tasks
        private volatile bool _isPaused = false;
        private volatile int _roundRobinIndex = 0;

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
            if (!_taskQueue.IsEmpty)
            {
                Task.Run(ProcessQueueIfNeeded);
            }
        }

        public bool IsPaused => _isPaused;

        public TaskDispatcher(
            IReadOnlyDictionary<string, DeviceProfile> deviceConfigs,
            CommandSender sender)
        {
            _deviceProfile = deviceConfigs;
            _sender = sender;
            _processingTask = ProcessQueueAsync(_cts.Token);
        }

        public void EnqueueTasks(List<TransportTask> tasks)
        {
            var duplicateList = new List<TransportTask>();

            foreach (TransportTask task in tasks)
            {
                if (_taskQueue.Any(existing => existing.TaskId == task.TaskId))
                {
                    duplicateList.Add(task);
                }
            }

            if (duplicateList.Count > 0)
            {
                throw new DuplicateTaskException(string.Join(", ", duplicateList));
            }

            foreach (TransportTask task in tasks)
            {
                _taskQueue.Enqueue(task);
            }
        }

        public async Task ProcessQueueIfNeeded()
        {
            if (_taskQueue.IsEmpty || _isPaused) return;
            await AssignTasksToIdleDevicesAsync();
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(200, token);
                if (!_taskQueue.IsEmpty && !_isPaused)
                {
                    await AssignTasksToIdleDevicesAsync();
                }
            }
        }

        private async Task AssignTasksToIdleDevicesAsync()
        {
            if (_taskQueue.IsEmpty) return;

            var idleDevices = await _sender._deviceMonitor.GetIdleDevicesAsync();

            while (!_taskQueue.IsEmpty)
            {
                if (_taskQueue.TryPeek(out TransportTask? task))
                {
                    DeviceProfile? suitableProfile = GetSuitableDeviceProfile(idleDevices, task);

                    if (suitableProfile == null)
                    {
                        break;
                    }

                    _taskQueue.TryDequeue(out _);

                    var deviceId = suitableProfile.Id;
                    _assigningDevices[deviceId] = task.TaskId;

                    await AssignTaskToDeviceAsync(task, suitableProfile, deviceId);
                }
            }
        }

        /// <summary>
        /// Lấy DeviceProfile phù hợp nhất cho một TransportTask từ danh sách các thiết bị idle. (Áp dụng cân bằng tải)
        /// 
        /// Hàm này áp dụng chiến lược kết hợp giữa sắp xếp dựa trên khoảng cách (distance-based sorting) và 
        /// lựa chọn luân phiên theo kiểu round-robin để đảm bảo load balancing tốt hơn giữa các thiết bị.
        /// 
        /// - Đầu tiên, lọc và sắp xếp các thiết bị idle theo khoảng cách gần nhất đến vị trí tham chiếu của task 
        ///   (ví dụ: gate cho Inbound hoặc source location cho Outbound), để ưu tiên hiệu quả di chuyển.
        /// - Sau đó, sử dụng chỉ số round-robin (_roundRobinIndex) để chọn luân phiên từ danh sách đã sắp xếp, 
        ///   tránh tình trạng luôn chọn thiết bị gần nhất dẫn đến overload (quá tải) cho một số thiết bị cụ thể, 
        ///   đồng thời đảm bảo tính công bằng (fairness) trong phân phối task giữa các thiết bị idle.
        /// 
        /// Vấn đề giải quyết: Tránh bias (thiên vị) về thiết bị gần nhất, giúp phân tải đều hơn, giảm thời gian chờ 
        /// và tăng hiệu suất tổng thể hệ thống khi có nhiều task và thiết bị.
        /// 
        /// Nếu không có thiết bị phù hợp, trả về null.
        /// </summary>
        /// <param name="idleDevices">Danh sách các thiết bị đang idle (rảnh rỗi).</param>
        /// <param name="task">TransportTask cần assign.</param>
        /// <returns>DeviceProfile của thiết bị được chọn, hoặc null nếu không có.</returns>
        private DeviceProfile? GetSuitableDeviceProfile(List<DeviceInfo> idleDevices, TransportTask task)
        {
            var candidates = idleDevices
                .Where(device => _deviceProfile.TryGetValue(device.DeviceId, out var p) && !_assigningDevices.ContainsKey(device.DeviceId))
                .OrderBy(device => CalculateDistance(device.Location, GetReferenceLocationForTask(task)))  // Vẫn sort distance trước
                .ToList();

            if (candidates.Count == 0) return null;

            int index = Interlocked.Increment(ref _roundRobinIndex) % candidates.Count;
            var selectedDevice = candidates[index];
            return _deviceProfile[selectedDevice.DeviceId];
        }

        private static Location GetReferenceLocationForTask(TransportTask task)
            => task.CommandType switch
            {
                CommandType.Inbound => _gateLocation,
                CommandType.Outbound => task.SourceLocation!,
                _ => task.SourceLocation!
            };

        private static int CalculateDistance(Location locationA, Location locationB) =>
            Math.Abs(locationA.Floor - locationB.Floor) +
            Math.Abs(locationA.Rail - locationB.Rail) +
            Math.Abs(locationA.Block - locationB.Block);

        private async Task AssignTaskToDeviceAsync(TransportTask task, DeviceProfile profile, string deviceId)
        {
            try
            {
                PlcConnector connector = await _sender._deviceMonitor.GetConnectorAsync(profile.Id);

                await TriggerCommandAsync(connector, profile.Signals, task);

                CancellationTokenSource pollingCts = new();
                Task pollTask = StartPollingTask(task, deviceId, connector, profile, pollingCts.Token);

                _sender.activePollTasks.TryAdd(task.TaskId, (pollTask, pollingCts));

                _ = pollTask.ContinueWith(t => _assigningDevices.TryRemove(deviceId, out _));
            }
            catch (DeviceNotRegisteredException)
            {
                _sender.TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, task.TaskId, ErrorDetail.DeviceNotRegistered(deviceId)));

                _sender.Pause();

                return;
            }
            catch (Exception ex)
            {
                _assigningDevices.TryRemove(deviceId, out _);
                _sender.TaskFailed?.Invoke(_sender, new TaskFailedEventArgs(deviceId, task.TaskId, ErrorDetail.TransportTaskAssignmentFailure(deviceId, task.TaskId, ex)));

                _sender.Pause();

            }
        }

        private static async Task TriggerCommandAsync(PlcConnector connector, SignalMap signals, TransportTask task)
        {
            await (task.CommandType switch
            {
                CommandType.Inbound => TriggerInboundCommand(connector, signals),
                CommandType.Outbound => TriggerOutboundCommand(connector, signals, task.SourceLocation!, task.GateNumber, task.OutDirBlock != Direction.Bottom),
                _ => TriggerTransferCommand(
                    connector,
                    signals,
                    task.SourceLocation!,
                    task.TargetLocation!,
                    task.GateNumber,
                    task.InDirBlock != Direction.Bottom,
                    task.OutDirBlock != Direction.Bottom)
            });
        }

        private Task StartPollingTask(TransportTask task, string deviceId, PlcConnector connector, DeviceProfile profile, CancellationToken token)
            => task.CommandType switch
            {
                CommandType.Inbound => _sender.StartInboundPolling(deviceId, connector, task.TaskId, token),
                CommandType.Outbound => _sender.StartOutboundPolling(deviceId, task.TaskId, connector, profile.Signals, profile.TimeoutMinutes, token),
                _ => _sender.StartTransferPolling(deviceId, task.TaskId, connector, profile.Signals, profile.TimeoutMinutes, token)
            };

        public void OnDeviceStatusChanged(object? senderArg, DeviceStatusChangedEventArgs args)
        {
            if (args.NewStatus == DeviceStatus.Idle && !_taskQueue.IsEmpty)
            {
                Task.Run(ProcessQueueIfNeeded);
            }
        }

        public TransportTask[] GetQueuedTasks() => [.. _taskQueue];

        public bool RemoveTasks(IEnumerable<string> taskIds)
        {
            if (IsPaused || !taskIds.Any())
            {
                return false;
            }

            var temp = new List<TransportTask>();
            var removedCount = 0;
            var taskIdSet = new HashSet<string>(taskIds);

            while (_taskQueue.TryDequeue(out TransportTask? task))
            {
                if (taskIdSet.Contains(task.TaskId))
                {
                    removedCount++;
                }
                else
                {
                    temp.Add(task);
                }
            }

            foreach (var task in temp)
            {
                _taskQueue.Enqueue(task);
            }

            return removedCount > 0;
        }

        public string? GetCurrentTask(string deviceId)
        {
            _assigningDevices.TryGetValue(deviceId, out string? value);

            return value;
        }

        public async void Dispose()
        {
            await _processingTask;
            _assigningDevices.Clear();
            _roundRobinIndex = 0;
            _isPaused = false;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
