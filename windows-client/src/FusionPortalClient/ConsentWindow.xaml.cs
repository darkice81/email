using System.Windows;

namespace FusionPortalClient;

public partial class ConsentWindow : Window
{
    public ConsentWindow()
    {
        InitializeComponent();
    }

    private void Agree_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Decline_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
