namespace TQG.Automation.SDK.Exceptions;

public sealed class PlcConnectionFailedException(string ipAddress, int maxRetries) 
    : Exception($"Failed to connect to PLC at {ipAddress} after {maxRetries} attempts.")
{
    public string IpAddress { get; } = ipAddress;
    public int MaxRetries { get; } = maxRetries;
}
