namespace FusionPortalClient.Models;

public class RunningProgram
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
}

public class TelemetrySnapshot
{
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string Hostname { get; set; } = string.Empty;
    public bool DomainJoined { get; set; }
    public string? DomainName { get; set; }
    public List<string> LocalIpAddresses { get; set; } = new();
    public string? PublicIpAddress { get; set; }
    public bool VpnDetected { get; set; }
    public List<string> VpnAdapterNames { get; set; } = new();
    public string? ActiveGame { get; set; }
    public List<RunningProgram> RunningPrograms { get; set; } = new();
}
