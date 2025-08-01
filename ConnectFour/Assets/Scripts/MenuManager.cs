using UnityEngine;
using TMPro;  // precisa disso pra TMP_InputField

/// <summary>
/// Gerencia o menu inicial onde o jogador escolhe ser Host ou Cliente.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Referências UI")]
    public GameObject painelMenu;              // painel com botões e input
    public TMP_InputField ipInput;             // campo para digitar o IP
    public GameManager gameManager;            // referência ao GameManager

    private void Start()
    {
        // O painel do menu deve começar ativo
        painelMenu.SetActive(true);

        // Verifica se o GameManager está na cena
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[MenuManager] GameManager não encontrado na cena!");
            }
        }

        // Associa o callback de receber jogada do outro jogador
        NetworkManager.Instance.OnReceivedMove = (coluna) =>
        {
            Debug.Log("[MenuManager] Recebeu jogada do outro jogador: coluna " + coluna);
            gameManager.OnReceivedMove(coluna);  // chama método no GameManager
        };
    }

    /// <summary>
    /// Chamado pelo botão "Criar Partida"
    /// </summary>
    public void OnHostButton()
    {
        painelMenu.SetActive(false); // esconde o menu
        NetworkManager.Instance.StartHost();
        Debug.Log("[Menu] Host iniciado!");
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

        painelMenu.SetActive(false); // esconde o menu
        NetworkManager.Instance.StartClient(ip);
        Debug.Log("[Menu] Conectando ao servidor em: " + ip);
    }
}
