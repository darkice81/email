using System.ComponentModel;
using System.Windows;
using FusionPortalClient.ViewModels;

namespace FusionPortalClient;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closing += MainWindow_Closing;
    }

    /// <summary>
    /// Minimizes to the tray instead of exiting, so the client keeps
    /// reporting/checking in during an open match session. Use the tray
    /// icon's "Exit" option to actually quit.
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
