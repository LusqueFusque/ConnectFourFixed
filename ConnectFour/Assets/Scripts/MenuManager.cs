using UnityEngine;
using TMPro;  // para TMP_InputField e TextMeshProUGUI
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Gerencia o menu inicial onde o jogador escolhe ser Host ou Cliente.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject painelMenu;              // painel com botões e input
    public TMP_InputField ipInput;             // campo para digitar o IP
    public TextMeshProUGUI ipText;             // texto que mostra o IP local do host
    public GameManager gameManager;            // referência ao GameManager

    private void Start()
    {
        painelMenu.SetActive(true);

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
                Debug.LogError("[MenuManager] GameManager não encontrado na cena!");
        }

        NetworkManager.Instance.OnReceivedMove = (coluna) =>
        {
            Debug.Log("[MenuManager] Recebeu jogada do outro jogador: coluna " + coluna);
            gameManager.OnReceivedMove(coluna);
        };
    }

    /// <summary>
    /// Chamado pelo botão "Criar Partida"
    /// </summary>
    public void OnHostButton()
    {
        painelMenu.SetActive(false);
        NetworkManager.Instance.StartHost();

        string localIP = GetLocalIPv4();
        if (ipText != null)
            ipText.text = "Seu IP: " + localIP;

        Debug.Log("[Menu] Host iniciado! IP local: " + localIP);
    }

    /// <summary>
    /// Chamado pelo botão "Conectar"
    /// </summary>
    public void OnConnectButton()
    {
        string ip = ipInput.text;
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("[Menu] IP não preenchido!");
            return;
        }

        painelMenu.SetActive(false);
        NetworkManager.Instance.StartClient(ip);
        Debug.Log("[Menu] Conectando ao servidor em: " + ip);
    }

    /// <summary>
    /// Retorna o primeiro IP IPv4 válido da interface de rede ativa, ignorando adaptadores virtuais e loopback
    /// </summary>
    private string GetLocalIPv4()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            string name = ni.Name.ToLower();
            if (name.Contains("virtual") || name.Contains("docker") || name.Contains("vpn"))
                continue;

            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address.ToString();
            }
        }
        return "127.0.0.1";
    }
}
