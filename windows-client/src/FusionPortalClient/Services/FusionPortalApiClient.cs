using System.Net.Http.Headers;
using System.Net.Http.Json;
using FusionPortalClient.Models;

namespace FusionPortalClient.Services;

public interface IFusionPortalApiClient
{
    void SetDeviceToken(string token);
    Task<string?> GetPublicIpAsync();
    Task<EnrollmentResult> EnrollAsync(string siteCode, string enrollmentCode, string hostname);
    Task<MatchInfo?> GetCurrentMatchAsync(string deviceId);
    Task<RosterResponse> GetRosterAsync(string matchId);
    Task<CheckInResult> CheckInAsync(string matchId, string playerId, string deviceId, TelemetrySnapshot telemetry);
    Task PostTelemetryAsync(string deviceId, TelemetrySnapshot telemetry);
}

/// <summary>
/// Thin HTTP client over the proposed contract in docs/API_CONTRACT.md.
/// Swap the route/shape details here once FusionPortal's real API is final.
/// </summary>
public class FusionPortalApiClient : IFusionPortalApiClient
{
    private readonly HttpClient _http;

    public FusionPortalApiClient(HttpClient http)
    {
        _http = http;
    }

    public void SetDeviceToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string?> GetPublicIpAsync()
    {
        try
        {
            var response = await _http.GetAsync("/api/v1/whoami");
            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<WhoAmIResponse>();
            return payload?.PublicIp;
        }
        catch (HttpRequestException)
        {
            // Best-effort; telemetry is still useful without the public IP.
            return null;
        }
    }

    private class WhoAmIResponse
    {
        public string? PublicIp { get; set; }
    }

    public async Task<EnrollmentResult> EnrollAsync(string siteCode, string enrollmentCode, string hostname)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/devices/enroll", new
        {
            siteCode,
            enrollmentCode,
            hostname
        });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EnrollmentResult>();
        return result ?? throw new InvalidOperationException("Enrollment response was empty.");
    }

    public async Task<MatchInfo?> GetCurrentMatchAsync(string deviceId)
    {
        var response = await _http.GetAsync($"/api/v1/devices/{deviceId}/current-match");
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MatchInfo>();
    }

    public async Task<RosterResponse> GetRosterAsync(string matchId)
    {
        var response = await _http.GetAsync($"/api/v1/matches/{matchId}/roster");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RosterResponse>();
        return result ?? new RosterResponse();
    }

    public async Task<CheckInResult> CheckInAsync(string matchId, string playerId, string deviceId, TelemetrySnapshot telemetry)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/matches/{matchId}/checkin", new
        {
            playerId,
            deviceId,
            telemetry
        });

        if (!response.IsSuccessStatusCode)
        {
            return new CheckInResult
            {
                Success = false,
                Error = $"Check-in failed: {(int)response.StatusCode} {response.ReasonPhrase}"
            };
        }

        var result = await response.Content.ReadFromJsonAsync<CheckInResult>();
        return result ?? new CheckInResult { Success = true, CheckedInAt = DateTime.UtcNow };
    }

    public async Task PostTelemetryAsync(string deviceId, TelemetrySnapshot telemetry)
    {
        var response = await _http.PostAsJsonAsync($"/api/v1/devices/{deviceId}/telemetry", telemetry);
        response.EnsureSuccessStatusCode();
    }
}
