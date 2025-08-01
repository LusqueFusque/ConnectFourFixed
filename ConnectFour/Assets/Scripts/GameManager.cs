using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI TurnText;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI vitoriaText;
    public GameObject telaVitoria;
    public Button botao;

    [Header("Prefabs")]
    public GameObject ficVermelha;
    public GameObject ficAmarela;

    private int[,] board = new int[7, 6]; // 0=vazio, 1=vermelho, 2=amarelo
    private float tamFicha = 1f;
    private int currentPlayer = 1;
    private bool acabou = false;
    private bool inputTravado = false;

    private void Start()
    {
        infoText.text = "Jogador Vermelho começa!";
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;
        telaVitoria.SetActive(false);

        botao.onClick.AddListener(Reinicio);
    }

    private void Update()
    {
        if (acabou || inputTravado) return;

        // Só deixa jogar se for sua vez
        bool isMyTurn = (NetworkManager.Instance.mode == NetworkManager.NetworkMode.Host && currentPlayer == 1)
                     || (NetworkManager.Instance.mode == NetworkManager.NetworkMode.Client && currentPlayer == 2);

        if (!isMyTurn) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int coluna = Mathf.RoundToInt(worldPos.x);

            if (coluna >= 0 && coluna < 7)
            {
                Jogar(coluna, enviarParaOutro: true);
            }
        }
    }

    /// <summary>
    /// Executa a jogada localmente e (opcional) envia para o outro jogador
    /// </summary>
    public void Jogar(int coluna, bool enviarParaOutro)
    {
        if (acabou || inputTravado) return;

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

                if (enviarParaOutro)
                {
                    NetworkManager.Instance.SendMove(coluna);
                }

                return;
            }
        }
    }

    /// <summary>
    /// Chamado pelo NetworkManager quando o outro jogador joga
    /// </summary>
    public void OnReceivedMove(int coluna)
    {
        Debug.Log("[GameManager] OnReceivedMove: coluna " + coluna);
        Jogar(coluna, enviarParaOutro: false);  // jogada recebida não reenvia
    }

    private System.Collections.IEnumerator AnimacaoFinal(GameObject disc, Vector3 destino, int coluna, int linha)
    {
        // anima descendo
        yield return StartCoroutine(anim(disc, destino));

        yield return new WaitForSeconds(0.2f); // delay visual

        if (SeraseVenceu(coluna, linha))
        {
            infoText.text = "";
            telaVitoria.SetActive(true);
            vitoriaText.text = "Jogador " + (currentPlayer == 1 ? "Vermelho" : "Amarelo") + " venceu!";
            TurnText.text = "";
            acabou = true;
        }
        else
        {
            currentPlayer = 3 - currentPlayer; // troca jogador
            TurnText.text = "Vez das fichas " + (currentPlayer == 1 ? "vermelhas" : "amarelas");
            TurnText.color = currentPlayer == 1 ? Color.red : Color.yellow;
            inputTravado = false;
        }
    }

    private bool SeraseVenceu(int col, int linha)
    {
        int player = board[col, linha];

        if (CountInDirection(col, linha, 1, 0, player) + CountInDirection(col, linha, -1, 0, player) >= 3) return true; // horizontal
        if (CountInDirection(col, linha, 0, 1, player) + CountInDirection(col, linha, 0, -1, player) >= 3) return true; // vertical
        if (CountInDirection(col, linha, 1, 1, player) + CountInDirection(col, linha, -1, -1, player) >= 3) return true; // diagonal \
        if (CountInDirection(col, linha, 1, -1, player) + CountInDirection(col, linha, -1, 1, player) >= 3) return true; // diagonal /

        return false;
    }

    private int CountInDirection(int startCol, int startLinha, int dirCol, int dirLinha, int player)
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

    private System.Collections.IEnumerator anim(GameObject disc, Vector3 target)
    {
        float speed = 15f;

        while (Vector3.Distance(disc.transform.position, target) > 0.01f)
        {
            disc.transform.position = Vector3.MoveTowards(disc.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        disc.transform.position = target;
    }

    /// <summary>
    /// Botão de reinício
    /// </summary>
    private void Reinicio()
    {
        telaVitoria.SetActive(false);
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;
        infoText.text = "Jogador Vermelho começa!";

        currentPlayer = 1;
        acabou = false;
        inputTravado = false;

        // limpa board visual
        foreach (GameObject ficha in GameObject.FindGameObjectsWithTag("Ficha"))
        {
            Destroy(ficha);
        }

        // limpa board lógico
        board = new int[7, 6];
    }
}
