using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Configuração do Portal")]
    [SerializeField] private string nomeDaCena;
    [SerializeField] private Vector2 spawnDestino;

    [Header("Configuração do Player")]
    [SerializeField] private float zDoPlayer = -1f;

    // Dados persistentes entre cenas
    private static Vector2 spawnSalvo;
    private static bool temSpawnSalvo = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Salva o spawn antes de trocar de cena
        spawnSalvo = spawnDestino;
        temSpawnSalvo = true;

        // Escuta o carregamento da nova cena
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(nomeDaCena);
    }

    private void OnSceneLoaded(Scene cena, LoadSceneMode mode)
    {
        if (!temSpawnSalvo) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            player.transform.position = new Vector3(
                spawnSalvo.x,
                spawnSalvo.y,
                zDoPlayer
            );
        }

        // Limpeza obrigatória
        SceneManager.sceneLoaded -= OnSceneLoaded;
        temSpawnSalvo = false;
    }
}
