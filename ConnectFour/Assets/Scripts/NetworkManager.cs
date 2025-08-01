using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public enum NetworkMode { None, Host, Client }
    public NetworkMode mode = NetworkMode.None;

    private TcpListener server;
    private TcpClient client;
    private Thread serverThread;
    private Thread clientThread;

    private NetworkStream stream;

    // Fila thread-safe para jogadas recebidas
    private ConcurrentQueue<int> jogadasRecebidas = new ConcurrentQueue<int>();

    // Evento para notificar GameManager da jogada recebida
    public Action<int> OnReceivedMove;

    // Porta fixa
    private const int PORT = 8080;

    #region Host (Servidor)

    public static NetworkManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject); // opcional, se quiser manter entre cenas
}

    public void StartHost()
    {
        if (mode != NetworkMode.None)
        {
            Debug.LogWarning("[NetworkManager] Já conectado ou servidor rodando!");
            return;
        }

        mode = NetworkMode.Host;

        serverThread = new Thread(ServerLoop);
        serverThread.IsBackground = true;
        serverThread.Start();

        Debug.Log("[NetworkManager] Servidor iniciado.");
    }

    private void ServerLoop()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, PORT);
            server.Start();

            client = server.AcceptTcpClient(); // aceita conexão do cliente
            stream = client.GetStream();

            Debug.Log("[NetworkManager] Cliente conectado.");

            while (mode == NetworkMode.Host)
            {
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(10);
                    continue;
                }

                byte[] buffer = new byte[1024];
                int length = stream.Read(buffer, 0, buffer.Length);

                if (length == 0)
                {
                    Debug.Log("[NetworkManager] Cliente desconectado.");
                    Stop();
                    break;
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, length);
                if (int.TryParse(msg, out int coluna))
                {
                    jogadasRecebidas.Enqueue(coluna);
                    Debug.Log("[NetworkManager] Jogada recebida do cliente: " + coluna);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[NetworkManager] Erro no servidor: " + ex.Message);
            Stop();
        }
    }

    #endregion

    #region Client (Cliente)

    public void StartClient(string ip)
    {
        if (mode != NetworkMode.None)
        {
            Debug.LogWarning("[NetworkManager] Já conectado ou servidor rodando!");
            return;
        }

        mode = NetworkMode.Client;

        clientThread = new Thread(() => ClientLoop(ip));
        clientThread.IsBackground = true;
        clientThread.Start();

        Debug.Log("[NetworkManager] Cliente iniciando conexão com: " + ip);
    }

    private void ClientLoop(string ip)
    {
        try
        {
            client = new TcpClient();
            client.Connect(IPAddress.Parse(ip), PORT);
            stream = client.GetStream();

            Debug.Log("[NetworkManager] Conectado ao servidor.");

            while (mode == NetworkMode.Client)
            {
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(10);
                    continue;
                }

                byte[] buffer = new byte[1024];
                int length = stream.Read(buffer, 0, buffer.Length);

                if (length == 0)
                {
                    Debug.Log("[NetworkManager] Servidor desconectado.");
                    Stop();
                    break;
                }

                string msg = Encoding.UTF8.GetString(buffer, 0, length);
                if (int.TryParse(msg, out int coluna))
                {
                    jogadasRecebidas.Enqueue(coluna);
                    Debug.Log("[NetworkManager] Jogada recebida do servidor: " + coluna);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[NetworkManager] Erro no cliente: " + ex.Message);
            Stop();
        }
    }

    #endregion

    #region Envio de dados

    public void SendMove(int coluna)
    {
        if (mode == NetworkMode.None)
        {
            Debug.LogWarning("[NetworkManager] Não está conectado.");
            return;
        }

        if (stream == null)
        {
            Debug.LogWarning("[NetworkManager] Stream não inicializado.");
            return;
        }

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(coluna.ToString());
            stream.Write(data, 0, data.Length);
            stream.Flush();

            Debug.Log("[NetworkManager] Jogada enviada: " + coluna);
        }
        catch (Exception ex)
        {
            Debug.LogError("[NetworkManager] Erro ao enviar jogada: " + ex.Message);
        }
    }

    #endregion

    private void Update()
    {
        // Processa as jogadas recebidas na thread principal
        while (jogadasRecebidas.TryDequeue(out int coluna))
        {
            OnReceivedMove?.Invoke(coluna);
        }
    }

    public void Stop()
    {
        mode = NetworkMode.None;

        try
        {
            stream?.Close();
            client?.Close();
            server?.Stop();
        }
        catch { }

        try
        {
            serverThread?.Abort();
        }
        catch { }

        try
        {
            clientThread?.Abort();
        }
        catch { }

        Debug.Log("[NetworkManager] Conexões e threads finalizadas.");
    }

    private void OnApplicationQuit()
    {
        Stop();
    }
}
