using UnityEngine;

public class BoardManager : MonoBehaviour
{

    public GameObject fichas;
    public int colunas = 7;
    public int linhas = 6;

    private void Start()
    {
        CriarBoard();
    }

    void CriarBoard()
    {
        float tamFicha = 1f;

        for (int y = 0; y < linhas; y++)
        {
            for (int x = 0; x < colunas; x++)
            {
                Vector3 pos = new Vector3(x * tamFicha, -y * tamFicha, 0);
                GameObject ficha = Instantiate(fichas, new Vector3(x, -y, 0), Quaternion.identity);
                ficha.transform.SetParent(transform);
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}