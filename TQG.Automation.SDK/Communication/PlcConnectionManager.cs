using S7.Net;
using TQG.Automation.SDK.Shared;

namespace TQG.Automation.SDK.Communication;

internal sealed class PlcConnectionManager : IDisposable
{
    public static readonly PlcConnectionManager Instance = new();

    private readonly Dictionary<string, PlcConnector> _connectors = [];
    private readonly object _lock = new();

    private PlcConnectionManager() { }

    public PlcConnector GetConnector(string ipAddress, PlcType plcType = PlcType.S71200, short rack = 0, short slot = 1)
    {
        lock (_lock)
        {
            if (_connectors.TryGetValue(ipAddress, out PlcConnector? connector))
            {
                return connector;
            }

            var newConnector = new PlcConnector(ipAddress, ToCpuType(plcType), rack, slot);
            _connectors[ipAddress] = newConnector;
            return newConnector;
        }
    }

    public bool Disconnect(string ipAddress)
    {
        lock (_lock)
        {
            if (_connectors.TryGetValue(ipAddress, out var connector))
            {
                _connectors.Remove(ipAddress);
                return true;
            }

            return false;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var connector in _connectors.Values)
            {
                connector.Dispose();
            }
            _connectors.Clear();
        }
    }

    private static CpuType ToCpuType(PlcType type)
    {
        return type switch
        {
            PlcType.S71200 => CpuType.S71200,
            PlcType.S71500 => CpuType.S71500,
            PlcType.S7300 => CpuType.S7300,
            PlcType.S7400 => CpuType.S7400,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported PLC type")
        };
    }
}
