using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using BeetleWire_UI.Services;
using Microsoft.UI.Xaml.Navigation;

namespace BeetleWire_UI.Pages;

public sealed partial class ClientPage : Page
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private string _sharedFolderPath;
    private MainWindow _mainWindow;

    public ClientPage()
    {
        this.InitializeComponent();
        LoadSharedFolderPath();
        EnsureSharedFolderExists();
    }

    private void LoadSharedFolderPath()
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        if (localSettings.Values.ContainsKey("SharedFolderPath"))
        {
            _sharedFolderPath = localSettings.Values["SharedFolderPath"].ToString();
        }
        else
        {
            _sharedFolderPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Shared");
        }
        SharedFolderPathTextBox.Text = _sharedFolderPath;
    }

    private void SaveSharedFolderPath()
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values["SharedFolderPath"] = _sharedFolderPath;
    }

    private void SetFolderButton_Click(object sender, RoutedEventArgs e)
    {
        _sharedFolderPath = SharedFolderPathTextBox.Text.Trim();
        if (string.IsNullOrEmpty(_sharedFolderPath))
        {
            StatusTextBlock.Text = "Folder path cannot be empty.";
            return;
        }
        SaveSharedFolderPath();
        EnsureSharedFolderExists();
        StatusTextBlock.Text = $"Shared folder set to: {_sharedFolderPath}";
    }

    private void EnsureSharedFolderExists()
    {
        if (!System.IO.Directory.Exists(_sharedFolderPath))
        {
            try
            {
                System.IO.Directory.CreateDirectory(_sharedFolderPath);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Failed to create folder: {ex.Message}";
            }
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var serverAddress = ServerAddressTextBox.Text;
        var uri = new Uri($"ws://{serverAddress}");
        try
        {
            await ConnectionManager.Instance.ConnectClientAsync(uri, _cts.Token);
            StatusTextBlock.Text = "Connected to server.";
            var fileList = await ReceiveFileListAsync();
            FilesListView.ItemsSource = fileList;

            _mainWindow?.UpdateClientStatus(true);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error connecting: {ex.Message}";
        }
    }

    private async Task<List<string>> ReceiveFileListAsync()
    {
        var buffer = new ArraySegment<byte>(new byte[4096]);
        WebSocketReceiveResult result = await _webSocket.ReceiveAsync(buffer, _cts.Token);
        string jsonString = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
        var fileList = JsonSerializer.Deserialize<List<string>>(jsonString);
        return fileList ?? new List<string>();
    }

    private async void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesListView.SelectedItem is string fileName)
        {
            StatusTextBlock.Text = $"Downloading {fileName}...";
            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadProgressBar.Value = 0;

            byte[] requestBytes = Encoding.UTF8.GetBytes(fileName);
            await _webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, _cts.Token);
            byte[] fileData = await ReceiveFileDataAsync();

            string filePath = System.IO.Path.Combine(_sharedFolderPath, fileName);
            System.IO.File.WriteAllBytes(filePath, fileData);

            StatusTextBlock.Text = $"File {fileName} downloaded to {_sharedFolderPath}.";
            DownloadProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private async Task<byte[]> ReceiveFileDataAsync()
    {
        var buffer = new ArraySegment<byte>(new byte[8192]);
        using (var ms = new System.IO.MemoryStream())
        {
            WebSocketReceiveResult result;
            do
            {
                result = await _webSocket.ReceiveAsync(buffer, _cts.Token);
                ms.Write(buffer.Array, buffer.Offset, result.Count);
            } while (!result.EndOfMessage);
            return ms.ToArray();
        }
    }

    private void SaveConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        var serverAddress = ServerAddressTextBox.Text.Trim();
        if (string.IsNullOrEmpty(serverAddress))
        {
            StatusTextBlock.Text = "Server address cannot be empty.";
            return;
        }

        var localSettings = ApplicationData.Current.LocalSettings;
        var connections = localSettings.Values["Connections"] as ApplicationDataCompositeValue ?? new ApplicationDataCompositeValue();
        connections[serverAddress] = DateTime.Now.ToString();
        localSettings.Values["Connections"] = connections;

        StatusTextBlock.Text = $"Connection to {serverAddress} saved.";
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }
        else
        {
            _mainWindow = null;
        }
    }
}