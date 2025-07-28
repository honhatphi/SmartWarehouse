using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TQG.Automation.Demo.Data;
using TQG.Automation.SDK.Events;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.Demo;

public class GatewayBackgroundService : BackgroundService
{
    private readonly AutomationGateway _gateway;
    private readonly MovingTaskRepository _repository;

    public event EventHandler<BarcodeReceivedEventArgs>? BarcodeReceived;
    public event EventHandler<TaskSucceededEventArgs>? TaskSucceeded;
    public event EventHandler<TaskFailedEventArgs>? TaskFailed;

    public GatewayBackgroundService(AutomationGateway gateway)
    {
        _gateway = gateway;

        _repository = MovingTaskRepository.Instance;

        _gateway.BarcodeReceived += OnBarcodeReceived;
        _gateway.TaskSucceeded += OnTaskSucceeded;
        _gateway.TaskFailed += OnTaskFailed;
    }

    private async void OnBarcodeReceived(object? sender, BarcodeReceivedEventArgs e)
    {
        BarcodeReceived?.Invoke(this, e);

        Direction direction = Direction.Bottom;
        short gateNumber = 1;
        Location? targetLocation = null;

        bool isValid = ValidateBarcodeAsync(e.TaskId, e.Barcode);
        if (isValid)
        {
            targetLocation = GetTargetLocationAsync(e.TaskId, e.Barcode);
        }

        await _gateway.SendValidationResult(e.DeviceId, e.TaskId, isValid, targetLocation, direction, gateNumber);


    }

    private void OnTaskSucceeded(object? sender, TaskSucceededEventArgs e)
    {

        TaskSucceeded?.Invoke(this, e);
    }

    private void OnTaskFailed(object? sender, TaskFailedEventArgs e)
    {
        TaskFailed?.Invoke(this, e);
    }


    private Location GetTargetLocationAsync(string taskId, string barcode)
    {
        List<MovingTask> movingTask = _repository.GetAll();

        var task = movingTask.FirstOrDefault(x => x.TaskId == taskId && x.Barcode == barcode);

        return task!.TargetLocation!;
    }

    private bool ValidateBarcodeAsync(string taskId, string barcode)
    {
        List<MovingTask> movingTask = _repository.GetAll();

        var task = movingTask.FirstOrDefault(x => x.TaskId == taskId && x.Barcode == barcode);

        if (task == null)
        {
            return false;
        }

        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

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

    public Task ActivateDevice(string deviceId) => _gateway.ActivateDevice(deviceId);

    public void DeactivateDevice(string deviceId) => _gateway.DeactivateDevice(deviceId);

    public bool IsConnected(string deviceId) => _gateway.IsConnected(deviceId);

    public async Task<Location?> GetActualLocationAsync(string deviceId) => await _gateway.GetActualLocationAsync(deviceId);

    public DeviceStatus GetDeviceStatus(string deviceId) => _gateway.GetDeviceStatus(deviceId);

    public async Task<List<DeviceInfo>> GetIdleDevicesAsync() => await _gateway.GetIdleDevicesAsync();

    public TransportTask[] GetPendingTask() => _gateway.GetPendingTask();

    public void PauseQueue() => _gateway.PauseQueue();

    public void ResumeQueue() => _gateway.ResumeQueue();

    public bool IsPauseQueue => _gateway.IsPauseQueue;

    public bool RemoveTransportTasks(IEnumerable<string> taskIds) => _gateway.RemoveTransportTasks(taskIds);

    public string? GetCurrentTask(string deviceId) => _gateway.GetCurrentTask(deviceId);
}
