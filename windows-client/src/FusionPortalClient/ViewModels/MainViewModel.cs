using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionPortalClient.Models;
using FusionPortalClient.Services;

namespace FusionPortalClient.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IFusionPortalApiClient _apiClient;
    private readonly TelemetryReportingService _telemetryReporting;
    private readonly string _deviceId;

    private MatchInfo? _currentMatch;
    private string _statusMessage = "Loading...";
    private RosterEntry? _selectedPlayer;

    public MainViewModel(
        IFusionPortalApiClient apiClient,
        TelemetryReportingService telemetryReporting,
        string deviceId)
    {
        _apiClient = apiClient;
        _telemetryReporting = telemetryReporting;
        _deviceId = deviceId;

        RefreshCommand = new RelayCommand(_ => RefreshAsync());
        CheckInCommand = new RelayCommand(_ => CheckInAsync(), _ => SelectedPlayer is { CheckedIn: false });
    }

    public ObservableCollection<RosterEntry> Roster { get; } = new();

    public MatchInfo? CurrentMatch
    {
        get => _currentMatch;
        private set
        {
            _currentMatch = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MatchTitleDisplay));
        }
    }

    public string MatchTitleDisplay => CurrentMatch?.Title ?? "No match scheduled";

    public RosterEntry? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            _selectedPlayer = value;
            OnPropertyChanged();
            CheckInCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand CheckInCommand { get; }

    public async Task RefreshAsync()
    {
        StatusMessage = "Checking for a scheduled match...";

        CurrentMatch = await _apiClient.GetCurrentMatchAsync(_deviceId);
        Roster.Clear();

        if (CurrentMatch is null)
        {
            StatusMessage = "No match currently scheduled for this device.";
            _telemetryReporting.Stop();
            return;
        }

        var roster = await _apiClient.GetRosterAsync(CurrentMatch.MatchId);
        foreach (var player in roster.Players)
            Roster.Add(player);

        StatusMessage = CurrentMatch.IsCheckInOpen
            ? $"Check-in is open for: {CurrentMatch.Title}"
            : $"Match found ({CurrentMatch.Title}), check-in not open yet.";

        if (CurrentMatch.IsCheckInOpen)
            _telemetryReporting.Start();
    }

    public async Task CheckInAsync()
    {
        if (CurrentMatch is null || SelectedPlayer is null)
            return;

        StatusMessage = "Checking in...";
        var snapshot = await _telemetryReporting.CaptureSnapshotAsync();
        var result = await _apiClient.CheckInAsync(CurrentMatch.MatchId, SelectedPlayer.PlayerId, _deviceId, snapshot);

        if (result.Success)
        {
            SelectedPlayer.CheckedIn = true;
            CheckInCommand.RaiseCanExecuteChanged();
            StatusMessage = $"Checked in at {result.CheckedInAt:t}.";
        }
        else
        {
            StatusMessage = result.Error ?? "Check-in failed.";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
