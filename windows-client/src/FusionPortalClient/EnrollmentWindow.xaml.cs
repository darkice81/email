using System.Windows;
using FusionPortalClient.Models;
using FusionPortalClient.Services;

namespace FusionPortalClient;

public partial class EnrollmentWindow : Window
{
    private readonly EnrollmentService _enrollmentService;

    public EnrollmentResult? Result { get; private set; }

    public EnrollmentWindow(EnrollmentService enrollmentService, string defaultSiteCode)
    {
        InitializeComponent();
        _enrollmentService = enrollmentService;
        SiteCodeBox.Text = defaultSiteCode;
    }

    private async void Enroll_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        var siteCode = SiteCodeBox.Text.Trim();
        var enrollmentCode = EnrollmentCodeBox.Text.Trim();

        if (string.IsNullOrEmpty(siteCode) || string.IsNullOrEmpty(enrollmentCode))
        {
            ErrorText.Text = "Both fields are required.";
            return;
        }

        try
        {
            Result = await _enrollmentService.EnrollAsync(siteCode, enrollmentCode);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Enrollment failed: {ex.Message}";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
