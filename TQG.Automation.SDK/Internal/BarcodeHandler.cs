using System.Collections.Concurrent;
using System.Threading.Channels;
using TQG.Automation.SDK.Communication;
using TQG.Automation.SDK.Events;
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

    public static async Task<string> ReadBarcodeAsync(PlcConnector connector, SignalMap signals)
    {
        var readTasks = new Task<string>[10]
        {
            connector.ReadAsync<string>(signals.BarcodeChar1),
            connector.ReadAsync<string>(signals.BarcodeChar2),
            connector.ReadAsync<string>(signals.BarcodeChar3),
            connector.ReadAsync<string>(signals.BarcodeChar4),
            connector.ReadAsync<string>(signals.BarcodeChar5),
            connector.ReadAsync<string>(signals.BarcodeChar6),
            connector.ReadAsync<string>(signals.BarcodeChar7),
            connector.ReadAsync<string>(signals.BarcodeChar8),
            connector.ReadAsync<string>(signals.BarcodeChar9),
            connector.ReadAsync<string>(signals.BarcodeChar10)
        };

        await Task.WhenAll(readTasks);

        string result = "";
        for (int i = 0; i < 10; i++)
        {
            string val = await readTasks[i];

            if (val.Length > 1)
            {
                break;
            }

            result += val;
        }

        return result;
    }

    public async Task SendBarcodeAsync(string deviceId, string taskId, string barcode)
    {
        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource();
        int timeOut = 5;
        _pendingResponses[taskId] = (deviceId, tcs, cts);

        try
        {
            var actualLocation = await _monitor.GetActualLocationAsync(deviceId);

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

    private Task ProcessValidationQueueAsync(Channel<BarcodeRequest> validationChannel)
        => Task.Run(async () =>
        {
            await foreach (var req in validationChannel.Reader.ReadAllAsync())
            {
                BarcodeReceived?.Invoke(this, new BarcodeReceivedEventArgs(req.DeviceId, req.TaskId, req.Barcode));
            }
        });

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