using BeetleWire_UI.Pages;
using BeetleWire_UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BeetleWire_UI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;

        ContentFrame.Navigate(typeof(ClientPage), this);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem selectedItem)
        {
            var tag = selectedItem.Tag.ToString();
            switch (tag)
            {
                case "client":
                    ContentFrame.Navigate(typeof(ClientPage), this);
                    break;
                case "server":
                    ContentFrame.Navigate(typeof(ServerPage), this);
                    break;
                case "connections":
                    ContentFrame.Navigate(typeof(ConnectionsPage));
                    break;
            }
        }
    }

    private void StopServerButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectionManager.Instance.StopServer();
        ServerStatusPanel.Visibility = Visibility.Collapsed;
    }

    private void DisconnectClientButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectionManager.Instance.DisconnectClient();
        ClientStatusPanel.Visibility = Visibility.Collapsed;
    }

    public void UpdateServerStatus(bool isRunning)
    {
        ServerStatusPanel.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
    }

    public void UpdateClientStatus(bool isConnected)
    {
        ClientStatusPanel.Visibility = isConnected ? Visibility.Visible : Visibility.Collapsed;
    }
}