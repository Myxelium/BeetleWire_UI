using BeetleWire_UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Navigation;

namespace BeetleWire_UI.Pages;

public sealed partial class ServerPage : Page
{
    private HttpListener _listener;
    private MainWindow _mainWindow;

    public ServerPage()
    {
        this.InitializeComponent();
    }

    private void StartServerButton_Click(object sender, RoutedEventArgs e)
    {
        string serverAddress = ServerAddressTextBox.Text.Trim();
        string dirPath = ServerDirectoryTextBox.Text.Trim();

        if (!Directory.Exists(dirPath))
        {
            Log("Directory does not exist: " + dirPath);
            return;
        }

        string prefix = $"http://{serverAddress}/";
        try
        {
            ConnectionManager.Instance.StartServer(prefix);
            Log($"Server listening on {serverAddress}");

            // Update the main window status
            _mainWindow.UpdateServerStatus(true);
        }
        catch (Exception ex)
        {
            Log("Failed to start listener: " + ex.Message);
        }
    }

    private async Task RunServerLoopAsync(string dirPath, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // Wait for an incoming HTTP request.
                var context = await _listener.GetContextAsync();

                // Check if it's a WebSocket request.
                if (context.Request.IsWebSocketRequest)
                {
                    _ = ProcessWebSocketRequestAsync(context, dirPath, token);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Log("Server loop error: " + ex.Message);
        }
    }

    private async Task ProcessWebSocketRequestAsync(HttpListenerContext context, string dirPath, CancellationToken token)
    {
        HttpListenerWebSocketContext wsContext = null;
        try
        {
            wsContext = await context.AcceptWebSocketAsync(null);
            Log("WebSocket connection established.");
        }
        catch (Exception ex)
        {
            Log("WebSocket handshake error: " + ex.Message);
            context.Response.StatusCode = 500;
            context.Response.Close();
            return;
        }

        WebSocket webSocket = wsContext.WebSocket;

        try
        {
            // List files in the directory.
            List<string> fileList = new List<string>();
            foreach (var file in Directory.EnumerateFiles(dirPath))
            {
                fileList.Add(Path.GetFileName(file));
            }
            string fileListJson = JsonSerializer.Serialize(fileList);
            byte[] listBytes = Encoding.UTF8.GetBytes(fileListJson);
            await webSocket.SendAsync(new ArraySegment<byte>(listBytes), WebSocketMessageType.Text, true, token);
            Log("Sent file list.");

            // Wait for the client to request a file.
            byte[] recvBuffer = new byte[4096];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(recvBuffer), token);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string requestedFile = Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
                Log($"Client requested file: {requestedFile}");
                string filePath = Path.Combine(dirPath, requestedFile);

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath, token);
                    await webSocket.SendAsync(new ArraySegment<byte>(fileBytes), WebSocketMessageType.Binary, true, token);
                    Log("Sent file data.");
                }
                else
                {
                    Log("Requested file not found.");
                }
            }
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", token);
            Log("WebSocket connection closed.");
        }
        catch (Exception ex)
        {
            Log("Error during WebSocket communication: " + ex.Message);
        }
    }

    // Helper to update the log (on UI thread).
    private void Log(string message)
    {
        DispatcherQueue.TryEnqueue(() => {
            LogTextBlock.Text += $"{DateTime.Now:T} - {message}\n";
        });
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