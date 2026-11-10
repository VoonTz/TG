
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneTrigger : MonoBehaviour
{
    [SerializeField] private string cutsceneSceneName = "CutsceneScene";

    private bool cutsceneStarted = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossDead") && !cutsceneStarted)
        {
            cutsceneStarted = true;

            Debug.Log("Boss derrotado! Carregando cutscene...");

            // Garante que o tempo esteja normal
            Time.timeScale = 1f;

            // Carrega a cena da cutscene
            SceneManager.LoadScene(cutsceneSceneName);
        }
    }
}
