using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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


    void Start()
    {
        infoText.text = "Jogador Vermelho começa!";
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;
        telaVitória.SetActive(false);
        botao.onClick.AddListener(Reinicio);
    }

    void Update()
    {
        if (acabou || inputTravado) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int coluna = Mathf.RoundToInt(worldPos.x);

            if (coluna >= 0 && coluna < 7)
            {
                PosicFicha(coluna);
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

    System.Collections.IEnumerator AnimacaoFinal(GameObject disc, Vector3 destino, int coluna, int linha)
    {
        // Anima a ficha descendo
        yield return StartCoroutine(anim(disc, destino));

        // Pequeno delay para dar tempo visual antes da vitória
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

        // horizontal, vertical, diagonal
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

    System.Collections.IEnumerator anim(GameObject disc, Vector3 target)
    {
        float speed = 15f;

        while (Vector3.Distance(disc.transform.position, target) > 0.01f)
        {
            disc.transform.position = Vector3.MoveTowards(disc.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        disc.transform.position = target;
    }

    void Reinicio()
    {
        telaVitória.SetActive(false);         // esconde a tela de vitória
        TurnText.text = "Vez das fichas vermelhas";
        TurnText.color = Color.red;
        infoText.text = "Jogador Vermelho começa!";

        currentPlayer = 1;
        acabou = false;
        inputTravado = false;

        // limpa o board visual e lógico
        foreach (GameObject ficha in GameObject.FindGameObjectsWithTag("Ficha"))
        {
            Destroy(ficha);
        }

        board = new int[7, 6]; // zera o tabuleiro lógico
    }
}