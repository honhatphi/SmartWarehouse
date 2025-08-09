using System.Collections.Concurrent;
using TQG.Automation.SDK.Communication;
using TQG.Automation.SDK.Events;
using TQG.Automation.SDK.Exceptions;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Internal;

internal sealed class DeviceMonitor(IReadOnlyDictionary<string, DeviceProfile> profiles) : IDisposable
{
    private readonly ConcurrentDictionary<string, DeviceStatus> deviceStatuses = new();

    public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

    public IReadOnlyDictionary<string, DeviceProfile> DeviceProfile => profiles;

    public async Task StartMonitoring(string deviceId)
    {
        if (GetDeviceStatus(deviceId) == DeviceStatus.Busy)
        {
            return;
        }

        DeviceProfile profile = GetProfile(deviceId);

        PlcConnector connector = await GetConnectorAsync(deviceId);

        bool isBusy = await connector.ReadAsync<bool>(profile.Signals.CommandAcknowledged);

        UpdateDeviceStatus(deviceId, isBusy ? DeviceStatus.Busy : DeviceStatus.Idle);

    }

    public void StopMonitoring(string deviceId)
    {
        DeviceProfile profile = GetProfile(deviceId);

        PlcConnectionManager.Instance.Disconnect(profile.IpAddress);

        deviceStatuses.TryRemove(deviceId, out _);

    }

    public bool IsConnected(string deviceId)
    {
        var status = GetDeviceStatus(deviceId);

        return status == DeviceStatus.Idle || status == DeviceStatus.Busy;
    }

    public DeviceStatus GetDeviceStatus(string deviceId)
        => deviceStatuses.GetValueOrDefault(deviceId, DeviceStatus.Offline);

    public void UpdateDeviceStatus(string deviceId, DeviceStatus newStatus)
    {
        var oldStatus = GetDeviceStatus(deviceId);

        if (oldStatus != newStatus)
        {
            deviceStatuses[deviceId] = newStatus;
            DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs(deviceId, newStatus));

        }
    }

    public bool ResetDeviceStatus(string deviceId)
    {
        var status = GetDeviceStatus(deviceId);
        if (status == DeviceStatus.Busy)
        {
            return false;
        }

        UpdateDeviceStatus(deviceId, DeviceStatus.Idle);
        return true;
    }

    public async Task<PlcConnector> GetConnectorAsync(string deviceId)
    {
        DeviceProfile profile = GetProfile(deviceId);
        var connector = PlcConnectionManager.Instance.GetConnector(profile.IpAddress, profile.Cpu, profile.Rack, profile.Slot);
        await connector.EnsureConnectedAsync();
        return connector;
    }

    public DeviceProfile GetProfile(string deviceId)
    {
        profiles.TryGetValue(deviceId, out var profile);

        if (profile is null)
        {
            throw new DeviceNotRegisteredException(deviceId);
        }

        return profile;
    }

    public async Task<List<DeviceInfo>> GetIdleDevicesAsync()
    {
        var idleDevices = new List<DeviceInfo>();

        foreach (var deviceId in DeviceProfile.Keys)
        {
            if (GetDeviceStatus(deviceId) != DeviceStatus.Idle)
            {
                continue;
            }

            try
            {
                DeviceProfile profile = GetProfile(deviceId);
                PlcConnector connector = await GetConnectorAsync(deviceId);
                SignalMap signals = profile.Signals;

                short floor = await connector.ReadAsync<short>(signals.ActualFloor);
                short rail = await connector.ReadAsync<short>(signals.ActualRail);
                short block = await connector.ReadAsync<short>(signals.ActualBlock);

                idleDevices.Add(new DeviceInfo(deviceId, new Location(floor, rail, block)));
            }
            catch (Exception)
            {
                return [];
            }
        }

        return idleDevices;
    }

    public async Task<Location?> GetActualLocationAsync(string deviceId)
    {
        try
        {
            var deviceStatus = GetDeviceStatus(deviceId);
            if (deviceStatus != DeviceStatus.Idle && deviceStatus != DeviceStatus.Busy)
            {
                return null;
            }

            DeviceProfile profile = GetProfile(deviceId);
            PlcConnector connector = await GetConnectorAsync(deviceId);
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

    public void Dispose() => deviceStatuses.Clear();
}