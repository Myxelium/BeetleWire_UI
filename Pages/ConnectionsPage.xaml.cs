using Windows.Storage;
using Microsoft.UI.Xaml.Controls;

namespace BeetleWire_UI.Pages;

public sealed partial class ConnectionsPage : Page
{
    public ConnectionsPage()
    {
        this.InitializeComponent();
        LoadConnections();
    }

    private void LoadConnections()
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        var connections = localSettings.Values["Connections"] as ApplicationDataCompositeValue;

        if (connections != null)
        {
            foreach (var connection in connections)
            {
                ConnectionsListView.Items.Add($"{connection.Key} - {connection.Value}");
            }
        }
    }
}