
namespace RxGUI.Common;

/// <summary>
/// Defines the JSON payload structure used for telemetry exchange.
/// </summary>
public sealed class TelemetryData
{
    /// <summary>Whether the airborne indicator is set.</summary>
    public bool m_airborneInd { get; set; }
    /// <summary>Hour component.</summary>
    public int m_hour { get; set; }
    /// <summary>Minute component.</summary>
    public int m_minute { get; set; }
    /// <summary>VCS value (can be empty).</summary>
    public string m_vcs { get; set; } = "";
    /// <summary>Source type numeric code.</summary>
    public int m_srcTN { get; set; }
    /// <summary>Latitude in degrees.</summary>
    public double m_latitude { get; set; }
    /// <summary>Longitude in degrees.</summary>
    public double m_longitude { get; set; }
    /// <summary>Altitude in configured units.</summary>
    public double m_altitude { get; set; }
    /// <summary>Course in degrees.</summary>
    public double m_course { get; set; }
    /// <summary>Speed in units.</summary>
    public double m_speed { get; set; }
}
