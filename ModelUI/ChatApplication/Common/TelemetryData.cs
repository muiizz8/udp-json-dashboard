namespace ChatApplication.Common;

public class TelemetryData
{
    public bool m_airborneInd { get; set; }
    public int m_hour { get; set; }
    public int m_minute { get; set; }
    public string m_vcs { get; set; } = "";
    public int m_srcTN { get; set; }
    public double m_latitude { get; set; }
    public double m_longitude { get; set; }
    public double m_altitude { get; set; }
    public double m_course { get; set; }
    public double m_speed { get; set; }
}
