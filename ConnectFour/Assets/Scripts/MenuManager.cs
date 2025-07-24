using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject painelMenu;    // painel com botões e input
    public TMPro.TMP_InputField ipInput; // campo para IP
    public GameManager gameManager;  // referência ao GameManager

    public void OnHostButton()
    {
        NetworkManager.Instance.StartHost();
        painelMenu.SetActive(false);
        gameManager.IniciarJogoComoHost();
    }

    public void OnConnectButton()
    {
        NetworkManager.Instance.serverIP = ipInput.text;
        NetworkManager.Instance.StartClient();
        painelMenu.SetActive(false);
        gameManager.IniciarJogoComoCliente();
    }
}