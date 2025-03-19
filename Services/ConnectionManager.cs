using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleWire_UI.Services;

public class ConnectionManager
{
    private static ConnectionManager _instance;
    public static ConnectionManager Instance => _instance ??= new ConnectionManager();

    public HttpListener ServerListener { get; private set; }
    public ClientWebSocket ClientSocket { get; private set; }

    private ConnectionManager() { }

    public void StartServer(string prefix)
    {
        if (ServerListener == null)
        {
            ServerListener = new HttpListener();
            ServerListener.Prefixes.Add(prefix);
            ServerListener.Start();
        }
    }

    public void StopServer()
    {
        ServerListener?.Stop();
        ServerListener = null;
    }

    public async Task ConnectClientAsync(Uri uri, CancellationToken token)
    {
        if (ClientSocket == null)
        {
            ClientSocket = new ClientWebSocket();
            await ClientSocket.ConnectAsync(uri, token);
        }
    }

    public void DisconnectClient()
    {
        ClientSocket?.Dispose();
        ClientSocket = null;
    }
}