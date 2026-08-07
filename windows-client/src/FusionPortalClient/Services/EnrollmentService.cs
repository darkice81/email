using System.IO;
using System.Text.Json;
using FusionPortalClient.Models;

namespace FusionPortalClient.Services;

/// <summary>
/// Minimal first-run device enrollment: exchanges an org-issued one-time
/// enrollment code for a device token, then caches it locally so the app
/// doesn't re-enroll on every launch. Replace with your organization's real
/// auth (OAuth device-code flow, certificate-based identity, etc.) before
/// production use - a long-lived static bearer token cached in plaintext on
/// disk is a placeholder, not a security design.
/// </summary>
public class EnrollmentService
{
    private readonly IFusionPortalApiClient _apiClient;
    private readonly ISystemInfoService _systemInfo;
    private readonly string _cacheFilePath;

    public EnrollmentService(IFusionPortalApiClient apiClient, ISystemInfoService systemInfo, string cacheFilePath)
    {
        _apiClient = apiClient;
        _systemInfo = systemInfo;
        _cacheFilePath = cacheFilePath;
    }

    public EnrollmentResult? TryLoadCached()
    {
        if (!File.Exists(_cacheFilePath))
            return null;

        try
        {
            var json = File.ReadAllText(_cacheFilePath);
            return JsonSerializer.Deserialize<EnrollmentResult>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<EnrollmentResult> EnrollAsync(string siteCode, string enrollmentCode)
    {
        var result = await _apiClient.EnrollAsync(siteCode, enrollmentCode, _systemInfo.GetHostname());

        Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath)!);
        File.WriteAllText(_cacheFilePath, JsonSerializer.Serialize(result));

        _apiClient.SetDeviceToken(result.DeviceToken);
        return result;
    }
}
