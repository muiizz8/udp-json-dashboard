namespace ChatCore.Models;

/// <summary>
/// Represents a single TCP instance configuration.
/// </summary>
public sealed class InstanceConfig
{
    /// <summary>Unique instance name.</summary>
    public string Name { get; set; } = "InstanceA";
    /// <summary>Local bind IP.</summary>
    public string LocalIp { get; set; } = "127.0.0.1";
    /// <summary>Local bind port.</summary>
    public string LocalPort { get; set; } = "9000";
    /// <summary>Remote target IP.</summary>
    public string RemoteIp { get; set; } = "127.0.0.1";
    /// <summary>Remote target port.</summary>
    public string RemotePort { get; set; } = "9001";
}
