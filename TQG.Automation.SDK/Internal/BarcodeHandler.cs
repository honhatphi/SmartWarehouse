using System.Collections.Concurrent;
using System.Threading.Channels;
using TQG.Automation.SDK.Communication;
using TQG.Automation.SDK.Events;
using TQG.Automation.SDK.Exceptions;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Internal;

internal sealed class BarcodeHandler : IDisposable
{
    private readonly DeviceMonitor _monitor;
    private readonly Channel<BarcodeRequest> _validationChannel;
    private readonly ConcurrentDictionary<string, (string DeviceId, TaskCompletionSource<bool> Tcs, CancellationTokenSource Cts)> _pendingResponses = new();
    private readonly Task _validationQueueTask;

    // Events
    public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    public BarcodeHandler(
        DeviceMonitor monitor,
        Channel<BarcodeRequest> validationChannel)
    {
        _monitor = monitor;
        _validationChannel = validationChannel;
        _validationQueueTask = ProcessValidationQueueAsync(validationChannel);
    }

    /// <summary>
    /// Đọc mã vạch từ thiết bị PLC.
    /// </summary>
    /// <param name="connector">Kết nối đến thiết bị PLC.</param>
    /// <param name="signals">Bản đồ tín hiệu của thiết bị.</param>
    /// <returns>Mã vạch đọc được dưới dạng chuỗi ký tự.</returns>
    public static async Task<string> ReadBarcodeAsync(PlcConnector connector, SignalMap signals)
    {
        var readTasks = new Task<int>[10]
        {
            connector.ReadAsync<int>(signals.BarcodeChar1),
            connector.ReadAsync<int>(signals.BarcodeChar2),
            connector.ReadAsync<int>(signals.BarcodeChar3),
            connector.ReadAsync<int>(signals.BarcodeChar4),
            connector.ReadAsync<int>(signals.BarcodeChar5),
            connector.ReadAsync<int>(signals.BarcodeChar6),
            connector.ReadAsync<int>(signals.BarcodeChar7),
            connector.ReadAsync<int>(signals.BarcodeChar8),
            connector.ReadAsync<int>(signals.BarcodeChar9),
            connector.ReadAsync<int>(signals.BarcodeChar10)
        };

        await Task.WhenAll(readTasks);

        var chars = new int[10];
        for (int i = 0; i < 10; i++)
        {
            chars[i] = await readTasks[i];
        }

        return string.Concat(chars);
    }

    public async Task SendBarcodeAsync(string deviceId, string taskId, string barcode)
    {
        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource();
        int timeOut = 2;
        _pendingResponses[taskId] = (deviceId, tcs, cts);

        try
        {
            var actualLocation = await GetActualLocationAsync(deviceId);

            await _validationChannel.Writer.WriteAsync(new BarcodeRequest
            {
                DeviceId = deviceId,
                TaskId = taskId,
                Barcode = barcode,
                ActualLocation = actualLocation
            });

            await tcs.Task.WaitAsync(TimeSpan.FromMinutes(timeOut), cts.Token);
        }
        finally
        {
            cts.Dispose();
            _pendingResponses.TryRemove(taskId, out _);
        }
    }

    public async Task SendValidationResult(
        string deviceId,
        string taskId,
        bool isValid,
        Location? targetLocation,
        Direction direction,
        short gateNumber)
    {
        try
        {
            if (!_pendingResponses.TryGetValue(taskId, out var entry))
            {
                TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.NotFoundTask(deviceId, taskId)));
                return;
            }

            if (entry.DeviceId != deviceId)
            {
                entry.Cts.Cancel();
                _pendingResponses.TryRemove(taskId, out _);

                TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.MismatchedDevice(taskId, entry.DeviceId, deviceId)));
                return;
            }

            _pendingResponses.TryRemove(taskId, out _);

            DeviceProfile profile = _monitor.GetProfile(deviceId);
            PlcConnector connector = await _monitor.GetConnectorAsync(deviceId);
            var signals = profile.Signals;
            bool inDirBlockValue = direction != Direction.Bottom;

            if (isValid && targetLocation != null)
            {
                await connector.WriteAsync(signals.BarcodeValid, true);
                await connector.WriteAsync(signals.BarcodeInvalid, false);

                await connector.WriteAsync(signals.TargetFloor, targetLocation.Floor);
                await connector.WriteAsync(signals.TargetRail, targetLocation.Rail);
                await connector.WriteAsync(signals.TargetBlock, targetLocation.Block);
                await connector.WriteAsync(signals.InDirBlock, inDirBlockValue);
                await connector.WriteAsync(signals.GateNumber, gateNumber);
            }
            else
            {
                await connector.WriteAsync(signals.BarcodeValid, false);
                await connector.WriteAsync(signals.BarcodeInvalid, true);
            }

            entry.Tcs.SetResult(true);

            entry.Cts.Dispose();
        }
        catch (DeviceNotRegisteredException)
        {
            TaskFailed?.Invoke(this, new TaskFailedEventArgs(deviceId, taskId, ErrorDetail.DeviceNotRegistered(deviceId)));
            return;
        }
        catch (Exception)
        {

            throw;
        }

    }

    private Task ProcessValidationQueueAsync(Channel<BarcodeRequest> validationChannel)
        => Task.Run(async () =>
        {
            await foreach (var req in validationChannel.Reader.ReadAllAsync())
            {
                BarcodeReceived?.Invoke(this, new BarcodeReceivedEventArgs(req.DeviceId, req.TaskId, req.Barcode));
            }
        });

    public async Task<Location?> GetActualLocationAsync(string deviceId)
    {
        try
        {
            var deviceStatus = _monitor.GetDeviceStatus(deviceId);
            if (deviceStatus != DeviceStatus.Idle || deviceStatus != DeviceStatus.Busy)
            {
                return null;
            }

            DeviceProfile profile = _monitor.GetProfile(deviceId);
            PlcConnector connector = await _monitor.GetConnectorAsync(deviceId);
            SignalMap signals = profile.Signals;

            short floor = await connector.ReadAsync<short>(signals.ActualFloor);
            short rail = await connector.ReadAsync<short>(signals.ActualRail);
            short block = await connector.ReadAsync<short>(signals.ActualBlock);

            return new Location(floor, rail, block);
        }
        catch (Exception)
        {
            return null;
        }

    }

    public async void Dispose()
    {
        await _validationQueueTask;

        foreach ((_, _, CancellationTokenSource Cts) in _pendingResponses.Values)
        {
            Cts.Cancel();
            Cts.Dispose();
        }
        _pendingResponses.Clear();
    }
}