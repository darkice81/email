using FusionPortalClient.Models;

namespace FusionPortalClient.Services;

/// <summary>
/// Builds a TelemetrySnapshot from the individual collectors and, while a
/// match session is open, posts one on a timer. Intentionally does not run
/// continuously in the background outside of an open match session/check-in
/// - see README "Why this design".
/// </summary>
public class TelemetryReportingService : IDisposable
{
    private readonly ISystemInfoService _systemInfo;
    private readonly IVpnDetectionService _vpnDetection;
    private readonly IGameDetectionService _gameDetection;
    private readonly IFusionPortalApiClient _apiClient;
    private readonly string _deviceId;
    private readonly TimeSpan _interval;
    private Timer? _timer;

    public TelemetryReportingService(
        ISystemInfoService systemInfo,
        IVpnDetectionService vpnDetection,
        IGameDetectionService gameDetection,
        IFusionPortalApiClient apiClient,
        string deviceId,
        TimeSpan interval)
    {
        _systemInfo = systemInfo;
        _vpnDetection = vpnDetection;
        _gameDetection = gameDetection;
        _apiClient = apiClient;
        _deviceId = deviceId;
        _interval = interval;
    }

    public async Task<TelemetrySnapshot> CaptureSnapshotAsync()
    {
        var (domainJoined, domainName) = _systemInfo.GetDomainInfo();
        var (vpnDetected, vpnAdapters) = _vpnDetection.DetectVpn();

        return new TelemetrySnapshot
        {
            CapturedAtUtc = DateTime.UtcNow,
            Hostname = _systemInfo.GetHostname(),
            DomainJoined = domainJoined,
            DomainName = domainName,
            LocalIpAddresses = _systemInfo.GetLocalIpAddresses(),
            PublicIpAddress = await _apiClient.GetPublicIpAsync(),
            VpnDetected = vpnDetected,
            VpnAdapterNames = vpnAdapters,
            ActiveGame = _gameDetection.DetectActiveGame(),
            RunningPrograms = _systemInfo.GetVisibleRunningPrograms()
        };
    }

    /// <summary>Starts periodic reporting. Call Stop() when the match session closes.</summary>
    public void Start()
    {
        _timer?.Dispose();
        _timer = new Timer(async _ => await ReportOnceAsync(), null, TimeSpan.Zero, _interval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async Task ReportOnceAsync()
    {
        try
        {
            var snapshot = await CaptureSnapshotAsync();
            await _apiClient.PostTelemetryAsync(_deviceId, snapshot);
        }
        catch (Exception)
        {
            // Best-effort periodic reporting; a single failed tick shouldn't crash the app.
            // A production build should log this via a proper logging framework.
        }
    }

    public void Dispose() => Stop();
}
