namespace FusionPortalClient.Models;

public class ApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 60;
}

public class AppConfig
{
    public ApiConfig FusionPortal { get; set; } = new();
}
