using S7.Net;
using System.ComponentModel;
using TQG.Automation.SDK.Exceptions;

namespace TQG.Automation.SDK.Communication;

/// <summary>
/// Lớp triển khai cụ thể việc giao tiếp với một PLC.
/// </summary>
/// <remarks>
/// Khởi tạo một connector mới với các thông số kết nối.
/// </remarks>
internal sealed class PlcConnector(string ipAddress, CpuType cpu, short rack, short slot) : IDisposable
{
    private readonly Plc _plc = new(cpu, ipAddress, rack, slot);

    public async Task<T> ReadAsync<T>(string address)
    {
        await EnsureConnectedAsync();

        object? value = await _plc.ReadAsync(address) ?? throw new InvalidOperationException($"Reading from address '{address}' returned null.");

        if (value is T t)
        {
            return t;
        }

        Type targetType = typeof(T);
        Type sourceType = value.GetType();

        if (!targetType.IsAssignableFrom(sourceType) && !CanConvert(sourceType, targetType))
        {
            throw new InvalidCastException($"Cannot convert value of type '{sourceType.Name}' from address '{address}' to '{targetType.Name}'.");
        }

        try
        {
            return (T)Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Conversion failed for value '{value}' from address '{address}' to type '{targetType.Name}'.", ex);
        }
    }

    public async Task WriteAsync<T>(string address, T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Cannot write a null value to the PLC.");
        }

        await EnsureConnectedAsync();

        try
        {
            await _plc.WriteAsync(address, value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to write value '{value}' to address '{address}'.", ex);
        }
    }

    public async Task EnsureConnectedAsync()
    {
        if (_plc.IsConnected)
        {
            return;
        }

        const int maxRetries = 3;
        const int delaySeconds = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _plc.OpenAsync();
                return;
            }
            catch (Exception)
            {
                if (attempt == maxRetries)
                {
                    throw new PlcConnectionFailedException(ipAddress, maxRetries);
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    private static bool CanConvert(Type sourceType, Type targetType)
    {
        if (sourceType == typeof(byte) && targetType == typeof(bool)) return true;
        if (sourceType == typeof(ushort) && targetType == typeof(int)) return true;
        if (sourceType == typeof(byte[]))
        {
            if (targetType == typeof(string)) return true;
            if (targetType.IsArray) return true;
        }

        var converter = TypeDescriptor.GetConverter(sourceType);
        return converter.CanConvertTo(targetType);
    }

    public void Dispose()
    {
        if (_plc?.IsConnected == true)
        {
            _plc.Close();
        }
    }
}