using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;  // Singleton para acesso global

    public enum NetworkMode { None, Host, Client }
    public NetworkMode mode = NetworkMode.None;

    private TcpListener server;
    private TcpClient client;
    private Thread serverThread;
    private Thread clientThread;

    private GameManager gameManager;

    public Action<int> OnReceivedMove;  // Evento para avisar quando receber jogada do outro jogador

    private void Awake()
    {
        // Garante que só existe um NetworkManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém ao trocar de cena (se tiver)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Pega referência ao GameManager na main thread
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[NetworkManager] GameManager não encontrado na cena!");
        }
    }

    /// <summary>
    /// Chamado pelo MenuManager quando jogador clica "Criar Partida"
    /// </summary>
    public void StartHost()
    {
        mode = NetworkMode.Host;

        serverThread = new Thread(ServerLoop);
        serverThread.IsBackground = true;
        serverThread.Start();

        Debug.Log("[Host] Servidor iniciado. Aguardando conexões...");
    }

    /// <summary>
    /// Chamado pelo MenuManager quando jogador clica "Conectar"
    /// </summary>
    public void StartClient(string serverIP)
    {
        mode = NetworkMode.Client;

        clientThread = new Thread(() => ClientLoop(serverIP));
        clientThread.IsBackground = true;
        clientThread.Start();

        Debug.Log("[Cliente] Tentando conectar ao servidor em: " + serverIP);
    }

    /// <summary>
    /// Loop do servidor (host)
    /// </summary>
    private void ServerLoop()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, 8080);
            server.Start();
            Debug.Log("[Host] Servidor escutando na porta 8080...");

            client = server.AcceptTcpClient();  // Espera o cliente conectar
            Debug.Log("[Host] Cliente conectado!");

            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Cliente desconectou

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("[Host] Recebido: " + msg);

                    // Espera que o cliente envie sempre um número (coluna)
                    if (int.TryParse(msg, out int coluna))
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            OnReceivedMove?.Invoke(coluna);
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Host] Erro no ServerLoop: " + ex.Message);
        }
    }

    /// <summary>
    /// Loop do cliente (conecta ao host)
    /// </summary>
    private void ClientLoop(string serverIP)
    {
        try
        {
            client = new TcpClient();
            client.Connect(serverIP, 8080);
            Debug.Log("[Cliente] Conectado ao servidor!");

            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Servidor desconectou

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("[Cliente] Recebido: " + msg);

                    if (int.TryParse(msg, out int coluna))
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            OnReceivedMove?.Invoke(coluna);
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Cliente] Erro no ClientLoop: " + ex.Message);
        }
    }

    /// <summary>
    /// Envia a jogada para o outro jogador
    /// </summary>
    public void SendMove(int coluna)
    {
        try
        {
            if (client != null && client.Connected)
            {
                string msg = coluna.ToString();
                byte[] data = Encoding.UTF8.GetBytes(msg);
                client.GetStream().Write(data, 0, data.Length);
                Debug.Log("[Network] Enviou coluna: " + coluna);
            }
            else
            {
                Debug.LogWarning("[Network] Não está conectado para enviar.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[Network] Erro ao enviar jogada: " + ex.Message);
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            server?.Stop();
            client?.Close();
            serverThread?.Abort();
            clientThread?.Abort();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Network] Erro ao fechar: " + ex.Message);
        }
    }
}
