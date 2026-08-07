using System.IO;
using System.Windows;
using FusionPortalClient.Models;
using FusionPortalClient.Services;
using FusionPortalClient.ViewModels;
using Microsoft.Extensions.Configuration;

namespace FusionPortalClient;

public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private TelemetryReportingService? _telemetryReporting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = LoadConfig();

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FusionPortalClient");
        Directory.CreateDirectory(appDataDir);

        var consentFilePath = Path.Combine(appDataDir, "consent.ok");
        if (!File.Exists(consentFilePath))
        {
            var consentWindow = new ConsentWindow();
            var agreed = consentWindow.ShowDialog() == true;
            if (!agreed)
            {
                Shutdown();
                return;
            }
            File.WriteAllText(consentFilePath, DateTime.UtcNow.ToString("O"));
        }

        var httpClient = new HttpClient { BaseAddress = new Uri(config.FusionPortal.BaseUrl) };
        var apiClient = new FusionPortalApiClient(httpClient);
        var systemInfo = new SystemInfoService();
        var vpnDetection = new VpnDetectionService();
        var gameDetection = new GameDetectionService(
            Path.Combine(AppContext.BaseDirectory, "Data", "KnownGames.json"));

        var enrollmentCachePath = Path.Combine(appDataDir, "device.json");
        var enrollmentService = new EnrollmentService(apiClient, systemInfo, enrollmentCachePath);

        var cached = enrollmentService.TryLoadCached();
        if (cached is null)
        {
            var enrollmentWindow = new EnrollmentWindow(enrollmentService, config.FusionPortal.SiteCode);
            var enrolled = enrollmentWindow.ShowDialog() == true;
            if (!enrolled || enrollmentWindow.Result is null)
            {
                Shutdown();
                return;
            }
            cached = enrollmentWindow.Result;
        }
        else
        {
            apiClient.SetDeviceToken(cached.DeviceToken);
        }

        _telemetryReporting = new TelemetryReportingService(
            systemInfo,
            vpnDetection,
            gameDetection,
            apiClient,
            cached.DeviceId,
            TimeSpan.FromSeconds(Math.Max(15, config.FusionPortal.PollIntervalSeconds)));

        var viewModel = new MainViewModel(apiClient, _telemetryReporting, cached.DeviceId);
        var mainWindow = new MainWindow(viewModel);

        SetupTrayIcon(mainWindow);

        MainWindow = mainWindow;
        mainWindow.Show();

        _ = viewModel.RefreshAsync();
    }

    private void SetupTrayIcon(Window window)
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "FusionPortal Client - reporting active"
        };

        _trayIcon.DoubleClick += (_, _) =>
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open FusionPortal Client", null, (_, _) =>
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        });
        contextMenu.Items.Add("Exit", null, (_, _) => Shutdown());
        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private static AppConfig LoadConfig()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var appConfig = new AppConfig();
        configuration.Bind(appConfig);
        return appConfig;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _telemetryReporting?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
