using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FusionPortalClient.Models;

public class MatchInfo
{
    public string MatchId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime CheckInOpensAt { get; set; }
    public DateTime CheckInClosesAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public bool IsCheckInOpen =>
        DateTime.UtcNow >= CheckInOpensAt && DateTime.UtcNow <= CheckInClosesAt;
}

public class RosterEntry : INotifyPropertyChanged
{
    private bool _checkedIn;

    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public bool CheckedIn
    {
        get => _checkedIn;
        set { _checkedIn = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class RosterResponse
{
    public List<RosterEntry> Players { get; set; } = new();
}

public class CheckInResult
{
    public bool Success { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? Error { get; set; }
}

public class EnrollmentResult
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
}
