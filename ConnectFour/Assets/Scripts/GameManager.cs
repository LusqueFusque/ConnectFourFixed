using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI TurnText;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI vitoriaText;

    public GameObject ficVermelha;
    public GameObject ficAmarela;
    public GameObject telaVitória;
    public Button botao;

    public int[,] board = new int[7, 6]; // 0 = vazio, 1 = vermelho, 2 = amarelo

    private float tamFicha = 1f;
    private int currentPlayer = 1;
    private bool acabou = false;
    private bool inputTravado = false;

    private bool minhaVez = false;

    void Start()
    {
        telaVitória.SetActive(false);
        botao.onClick.AddListener(Reinicio);
        infoText.text = "Escolha criar ou conectar no menu.";
        TurnText.text = "";
    }

    public void IniciarJogoComoHost()
    {
        currentPlayer = 1;
        minhaVez = true;
        acabou = false;
        inputTravado = false;

        infoText.text = "Você é o jogador Vermelho! Sua vez!";
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;

        board = new int[7, 6]; // reset tabuleiro lógico
        NetworkManager.Instance.OnReceivedMove += OnReceivedMove;
    }

    public void IniciarJogoComoCliente()
    {
        currentPlayer = 1;
        minhaVez = false;
        acabou = false;
        inputTravado = false;

        infoText.text = "Você é o jogador Amarelo! Aguardando o outro jogar...";
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;

        board = new int[7, 6]; // reset tabuleiro lógico
        NetworkManager.Instance.OnReceivedMove += OnReceivedMove;
    }

    void Update()
    {
        if (acabou || inputTravado || !minhaVez) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int coluna = Mathf.RoundToInt(worldPos.x);

            if (coluna >= 0 && coluna < 7)
            {
                PosicFicha(coluna);
                NetworkManager.Instance.SendMove(coluna);
                minhaVez = false;
            }
        }
    }

    void PosicFicha(int coluna)
    {
        for (int linha = 5; linha >= 0; linha--)
        {
            if (board[coluna, linha] == 0)
            {
                board[coluna, linha] = currentPlayer;

                GameObject prefab = currentPlayer == 1 ? ficVermelha : ficAmarela;
                Vector3 spawnPos = new Vector3(coluna * tamFicha, 6f, 0);
                Vector3 targetPos = new Vector3(coluna * tamFicha, -linha * tamFicha, 0);

                GameObject disc = Instantiate(prefab, spawnPos, Quaternion.identity);
                inputTravado = true;
                StartCoroutine(AnimacaoFinal(disc, targetPos, coluna, linha));
                return;
            }
        }
    }

    IEnumerator AnimacaoFinal(GameObject disc, Vector3 destino, int coluna, int linha)
    {
        yield return StartCoroutine(anim(disc, destino));
        yield return new WaitForSeconds(0.2f);

        if (SeraseVenceu(coluna, linha))
        {
            infoText.text = "";
            telaVitória.SetActive(true);
            vitoriaText.text = "Jogador " + (currentPlayer == 1 ? "Vermelho" : "Amarelo") + " venceu!";
            TurnText.text = "";
            acabou = true;
        }
        else
        {
            currentPlayer = 3 - currentPlayer;
            TurnText.text = "Vez das fichas " + (currentPlayer == 1 ? "vermelhas" : "amarelas");
            TurnText.color = currentPlayer == 1 ? Color.red : Color.yellow;
            inputTravado = false;
        }
    }

    bool SeraseVenceu(int col, int linha)
    {
        int player = board[col, linha];

        if (CountInDirection(col, linha, 1, 0, player) + CountInDirection(col, linha, -1, 0, player) >= 3) return true;
        if (CountInDirection(col, linha, 0, 1, player) + CountInDirection(col, linha, 0, -1, player) >= 3) return true;
        if (CountInDirection(col, linha, 1, 1, player) + CountInDirection(col, linha, -1, -1, player) >= 3) return true;
        if (CountInDirection(col, linha, 1, -1, player) + CountInDirection(col, linha, -1, 1, player) >= 3) return true;

        return false;
    }

    int CountInDirection(int startCol, int startLinha, int dirCol, int dirLinha, int player)
    {
        int count = 0;
        int col = startCol + dirCol;
        int linha = startLinha + dirLinha;

        while (col >= 0 && col < 7 && linha >= 0 && linha < 6 && board[col, linha] == player)
        {
            count++;
            col += dirCol;
            linha += dirLinha;
        }

        return count;
    }

    IEnumerator anim(GameObject disc, Vector3 target)
    {
        float speed = 15f;

        while (Vector3.Distance(disc.transform.position, target) > 0.01f)
        {
            disc.transform.position = Vector3.MoveTowards(disc.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        disc.transform.position = target;
    }

    public void Reinicio()
    {
        telaVitória.SetActive(false);
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;
        infoText.text = "Jogador Vermelho começa!";

        currentPlayer = 1;
        acabou = false;
        inputTravado = false;
        minhaVez = (NetworkManager.Instance.mode == NetworkMode.Host);

        foreach (GameObject ficha in GameObject.FindGameObjectsWithTag("Ficha"))
        {
            Destroy(ficha);
        }

        board = new int[7, 6];
    }

    // Método chamado ao receber jogada pela rede
    void OnReceivedMove(int coluna)
    {
        // Executar no main thread (use UnityMainThreadDispatcher se tiver)
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            PosicFicha(coluna);
            minhaVez = true;
            infoText.text = "Sua vez!";
        });
    }
}
