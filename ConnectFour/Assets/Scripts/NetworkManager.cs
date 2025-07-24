using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Enum para definir se é servidor ou cliente
public enum NetworkMode { None, Host, Client }

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance; // singleton para fácil acesso
    public NetworkMode mode = NetworkMode.None;
    public string serverIP = "127.0.0.1"; // IP que o cliente vai usar para conectar
    public int port = 8080;

    private TcpListener listener;
    private TcpClient client;
    private NetworkStream stream;

    private Thread netThread;
    private bool running = false;

    public Action<int> OnReceivedMove; // evento chamado ao receber jogada

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // mantém ao trocar de cena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartHost()
    {
        mode = NetworkMode.Host;
        running = true;
        netThread = new Thread(HostThread);
        netThread.IsBackground = true;
        netThread.Start();
    }

    public void StartClient()
    {
        mode = NetworkMode.Client;
        running = true;
        netThread = new Thread(ClientThread);
        netThread.IsBackground = true;
        netThread.Start();
    }

    private void HostThread()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Debug.Log("[Host] Aguardando conexão...");
            client = listener.AcceptTcpClient();
            stream = client.GetStream();
            Debug.Log("[Host] Cliente conectado!");

            ListenLoop();
        }
        catch (Exception e)
        {
            Debug.LogError("[Host] Erro: " + e.Message);
        }
    }

    private void ClientThread()
    {
        try
        {
            Debug.Log("[Cliente] Conectando ao servidor...");
            client = new TcpClient(serverIP, port);
            stream = client.GetStream();
            Debug.Log("[Cliente] Conectado!");

            ListenLoop();
        }
        catch (Exception e)
        {
            Debug.LogError("[Cliente] Erro: " + e.Message);
        }
    }

    private void ListenLoop()
    {
        byte[] buffer = new byte[1024];
        while (running)
        {
            try
            {
                if (stream.CanRead && stream.DataAvailable)
                {
                    int len = stream.Read(buffer, 0, buffer.Length);
                    if (len > 0)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, len);
                        int coluna = int.Parse(msg);
                        Debug.Log("[Network] Jogada recebida: coluna " + coluna);

                        OnReceivedMove?.Invoke(coluna); // chama evento
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Network] Erro no loop: " + e.Message);
                running = false;
            }

            Thread.Sleep(10); // evitar CPU 100%
        }
    }

    public void SendMove(int coluna)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] msg = Encoding.UTF8.GetBytes(coluna.ToString());
            try
            {
                stream.Write(msg, 0, msg.Length);
                Debug.Log("[Network] Jogada enviada: coluna " + coluna);
            }
            catch (Exception e)
            {
                Debug.LogError("[Network] Erro ao enviar: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        netThread?.Abort();
        stream?.Close();
        client?.Close();
        listener?.Stop();
    }
}
